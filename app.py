# -*- coding: utf-8 -*-
import os
import time
from dataclasses import dataclass
from typing import List, Dict, Any, Optional

import gradio as gr
from fastapi import FastAPI
from dotenv import load_dotenv
from sqlalchemy import create_engine, text
from psycopg2.extras import execute_values
from pgvector.psycopg2 import register_vector

from langchain_openai import OpenAIEmbeddings

# -------------------------------------------------
# Load ENV
# -------------------------------------------------
load_dotenv(override=True)

OPENAI_API_KEY = os.getenv("OPENAI_API_KEY")
DATABASE_URL = os.getenv("DATABASE_URL")      # DB có pgvector (persist embeddings)
DATABASE_URL_2 = os.getenv("DATABASE_URL_2")  # DB nguồn majors để SELECT

# Embedding model & dim (khớp pgvector column)
EMBED_MODEL = os.getenv("EMBED_MODEL", "text-embedding-3-small").strip()
if "large" in EMBED_MODEL:
    EMBED_DIM = 3072
else:
    EMBED_DIM = 1536

if not DATABASE_URL:
    raise RuntimeError("Thiếu biến môi trường DATABASE_URL trong .env")
if not DATABASE_URL_2:
    raise RuntimeError("Thiếu biến môi trường DATABASE_URL_2 trong .env")
if not OPENAI_API_KEY:
    raise RuntimeError("Thiếu biến môi trường OPENAI_API_KEY trong .env")

# SQLAlchemy engines
engine = create_engine(DATABASE_URL_2, pool_pre_ping=True)       # đọc majors
engine_vector = create_engine(DATABASE_URL, pool_pre_ping=True)  # lưu embeddings

# -------------------------------------------------
# Data model & Loader (DB -> Child Majors từ SELECT mới)
# -------------------------------------------------
@dataclass
class Major:
    major_code: str
    major_name: str
    description: str
    parent_code: str
    parent_name: str


SQL = """
SELECT 
  p.major_code AS parent_code,
  p.major_name AS parent_name,
  c.major_code AS child_code,
  c.major_name AS child_name,
  COALESCE(c.description, '') AS child_description
FROM public.majors AS c
JOIN public.majors AS p
  ON c.parent_major_id = p.major_id
WHERE c.is_active = TRUE
  AND p.is_active = TRUE
  AND EXISTS (
      SELECT 1
      FROM public.syllabus s
      WHERE s.major_id = c.major_id
        AND s.version_label = 'K19'
        AND s.is_active = TRUE
  )
ORDER BY p.major_name, c.major_name;
"""


def load_majors() -> List[Major]:
    with engine.connect() as conn:
        rows = list(conn.execute(text(SQL)).mappings())
    return [
        Major(
            major_code=r["child_code"],
            major_name=r["child_name"],
            description=(
                f"{r['child_description']}".strip()
                + "\n\nPARENT_MAJOR_CODE: " + r["parent_code"]
                + "\nPARENT_MAJOR_NAME: " + r["parent_name"]
                + "\nRELATION: CHILD_OF"
            ),
            parent_code=r["parent_code"],
            parent_name=r["parent_name"],
        )
        for r in rows
    ]


# -------------------------------------------------
# PGVector Helpers (persist DB, không dùng folder)
# -------------------------------------------------
EMBED_TABLE = "major_embeddings"


def _ensure_pgvector_and_table():
    with engine_vector.begin() as conn:
        # Cố tạo extension, nếu thiếu pgvector sẽ WARN
        try:
            conn.exec_driver_sql("CREATE EXTENSION IF NOT EXISTS vector;")
        except Exception as ex:
            print(f"[WARN] CREATE EXTENSION vector: {ex}")

        conn.exec_driver_sql(
            f"""
            CREATE TABLE IF NOT EXISTS {EMBED_TABLE} (
              major_code VARCHAR(64) PRIMARY KEY,
              major_name TEXT NOT NULL,
              content    TEXT NOT NULL,
              embedding  VECTOR({EMBED_DIM}) NOT NULL
            );
            """
        )


def _get_dbapi_conn(raw_conn):
    """Ưu tiên driver_connection (SA 2.x). Chỉ fallback sang .connection khi bắt buộc."""
    try:
        return raw_conn.driver_connection
    except Exception:
        try:
            return raw_conn.connection
        except Exception:
            return raw_conn


def _vec_literal(vec: List[float]) -> str:
    return "[" + ",".join(f"{float(x):.8f}".rstrip("0").rstrip(".") for x in vec) + "]"


def _upsert_major_embeddings(majors: List[Major]):
    """Tính embedding (OpenAI) và upsert vào Postgres (pgvector)."""
    if not majors:
        return

    texts = [f"{m.major_name}\n\n{m.description}" for m in majors]

    embedder = OpenAIEmbeddings(model=EMBED_MODEL)
    vectors = embedder.embed_documents(texts)
    vector_literals = [_vec_literal(v) for v in vectors]

    raw_conn = engine_vector.raw_connection()
    try:
        dbapi_conn = _get_dbapi_conn(raw_conn)
        register_vector(dbapi_conn)

        with dbapi_conn:
            with dbapi_conn.cursor() as cur:
                sql = f"""
                INSERT INTO {EMBED_TABLE} (major_code, major_name, content, embedding)
                VALUES %s
                ON CONFLICT (major_code) DO UPDATE
                SET major_name = EXCLUDED.major_name,
                    content    = EXCLUDED.content,
                    embedding  = EXCLUDED.embedding;
                """
                rows = [
                    (m.major_code, m.major_name, texts[i], vector_literals[i])
                    for i, m in enumerate(majors)
                ]
                execute_values(
                    cur,
                    sql,
                    rows,
                    page_size=200,
                    template="(%s, %s, %s, %s::vector)",
                )
    finally:
        raw_conn.close()


# -------------------------------------------------
# Gradio API: gọi là gen embeddings
# -------------------------------------------------
def build_major_embeddings(max_rows: Optional[int] = None) -> Dict[str, Any]:
    """
    API: Load majors -> ensure table -> embed -> upsert.
    Trả JSON status để dùng như API (HF/Gradio).
    """
    t0 = time.time()
    try:
        _ensure_pgvector_and_table()

        majors = load_majors()
        total = len(majors)

        if max_rows and int(max_rows) > 0:
            majors = majors[: int(max_rows)]

        _upsert_major_embeddings(majors)

        return {
            "ok": True,
            "table": EMBED_TABLE,
            "embed_model": EMBED_MODEL,
            "embed_dim": EMBED_DIM,
            "selected_rows": len(majors),
            "total_rows_in_source": total,
            "elapsed_sec": round(time.time() - t0, 3),
            "message": "Upsert major embeddings thành công.",
        }
    except Exception as ex:
        return {
            "ok": False,
            "table": EMBED_TABLE,
            "embed_model": EMBED_MODEL,
            "embed_dim": EMBED_DIM,
            "elapsed_sec": round(time.time() - t0, 3),
            "error": str(ex),
            "hint": "Nếu báo lỗi 'vector' / 'VECTOR(...)', kiểm tra pgvector đã được cài trong Postgres.",
        }


def health() -> Dict[str, Any]:
    return {
        "ok": True,
        "service": "major-embeddings-builder",
        "table": EMBED_TABLE,
        "embed_model": EMBED_MODEL,
        "embed_dim": EMBED_DIM,
    }


# FastAPI app for REST API endpoints
fastapi_app = FastAPI(title="Major Embeddings Builder API")


@fastapi_app.post("/api/build_major_embeddings")
async def api_build_major_embeddings(request: List[Optional[int]]) -> Dict[str, Any]:
    """
    FastAPI endpoint for building major embeddings.
    Accepts JSON body as array: [max_rows] where max_rows can be int or null
    """
    max_rows = request[0] if request and len(request) > 0 else None
    result = build_major_embeddings(max_rows)
    return result


@fastapi_app.get("/api/health")
async def api_health() -> Dict[str, Any]:
    """FastAPI endpoint for health check"""
    return health()


# Gradio Blocks UI
with gr.Blocks(title="Major Embeddings Builder (pgvector)") as demo:
    gr.Markdown("## Major Embeddings Builder (pgvector)\nGọi API để *generate & upsert* embeddings vào Postgres.")

    with gr.Row():
        max_rows = gr.Number(value=None, label="max_rows (optional)", precision=0)
    out = gr.JSON(label="Result")

    with gr.Row():
        btn_build = gr.Button("Build / Upsert Embeddings", variant="primary")
        btn_health = gr.Button("Health")

    # Expose dạng API
    btn_build.click(
        fn=build_major_embeddings,
        inputs=[max_rows],
        outputs=[out],
        api_name="build_major_embeddings",
    )
    btn_health.click(
        fn=health,
        inputs=[],
        outputs=[out],
        api_name="health",
    )

if __name__ == "__main__":
    # Mount FastAPI routes into Gradio app
    # This allows both Gradio UI (at /) and FastAPI endpoints (at /api/*) to work together
    combined_app = gr.mount_gradio_app(fastapi_app, demo, path="/")
    
    # Launch using uvicorn to properly handle both FastAPI and Gradio
    import uvicorn
    uvicorn.run(combined_app, host="0.0.0.0", port=int(os.getenv("PORT", "7860")))