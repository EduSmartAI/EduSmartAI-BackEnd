# EduSmart Backend - Hệ Thống Học Tập Thông Minh Dựa Trên AI

[![.NET Version](https://img.shields.io/badge/.NET-9.0-blue)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/Architecture-Microservices-green)](https://microservices.io/)
[![License](https://img.shields.io/badge/License-Educational-orange)](LICENSE)

##  Tổng Quan Tổng Thể

**EduSmart Backend** là một hệ thống backend phức tạp và quy mô lớn được xây dựng để phục vụ nền tảng học tập thông minh, tận dụng sức mạnh của **Artificial Intelligence (AI)** để cá nhân hóa trải nghiệm học tập cho từng sinh viên dựa trên năng lực, sở thích và mục tiêu cá nhân của họ.

###  Đặc Điểm Nổi Bật

- **️ Kiến trúc Microservices hiện đại**: 10 services độc lập, loosely coupled
- ** Event-Driven Architecture**: Giao tiếp bất đồng bộ qua RabbitMQ
- ** AI-Powered**: Tích hợp OpenAI GPT-4o-mini cho personalization
- ** CQRS Pattern**: Tách biệt Read/Write models để tối ưu hiệu suất
- **️ Dual Database Strategy**: PostgreSQL (write) + Marten/MongoDB (read)
- ** OAuth 2.0/OpenID Connect**: Authentication với OpenIddict
- ** Containerized**: Full Docker support với Docker Compose
- ** Scalable**: Horizontal scaling cho từng service
- **️ Resilient**: Outbox pattern, retry policies, circuit breakers
- ** Well-documented**: Extensive inline comments và API docs

###  Mục Đích Và Giá Trị Kinh Doanh

#### 1. **Đánh Giá Năng Lực Sinh Viên Toàn Diện**
- **Placement Tests**: Đánh giá kiến thức lý thuyết qua quiz nhiều lựa chọn
- **Practice Tests**: Đánh giá kỹ năng lập trình qua coding challenges
- **Surveys**: Phân tích thói quen học tập và sở thích nghề nghiệp
- **Transcript Analysis**: Kết hợp với bảng điểm để đánh giá chính xác hơn
- **Multi-dimensional Evaluation**: 
  - Quiz Score (60%): Kiến thức lý thuyết
  - Practice Score (40%): Kỹ năng thực hành
  - Transcript Score (20%): Nền tảng đã học

#### 2. **Tạo Lộ Trình Học Tập Cá Nhân Hóa Với AI**
- **AI-Powered Analysis**: Sử dụng GPT-4o-mini để phân tích toàn bộ dữ liệu sinh viên
- **Adaptive Learning Path**: Lộ trình thay đổi dựa trên tiến độ thực tế
- **Goal-Oriented**: Căn cứ vào mục tiêu nghề nghiệp (Backend Dev, AI Engineer, Mobile Dev...)
- **Time-Based Planning**: Tính toán dựa trên số giờ học/tuần sinh viên có thể dành ra
- **Difficulty Matching**: Khóa học phù hợp với level (Beginner/Intermediate/Advanced)
- **Prerequisite Handling**: Tự động sắp xếp thứ tự môn học theo tiên quyết

#### 3. **Quản Lý Nội Dung Khóa Học Phong Phú**
- **Multi-level Structure**: Course → Module → Lesson → Content
- **Rich Content Types**: Video, Document, Quiz, Assignment, Discussion
- **Progress Tracking**: Real-time theo dõi tiến độ từng bài học
- **Interactive Features**: Comments, Notes, Discussions
- **Assessment Integration**: Quiz và tests tích hợp trong khóa học

#### 4. **Hỗ Trợ Thanh Toán Và Thương Mại**
- **Shopping Cart**: Quản lý giỏ hàng
- **Order Management**: Xử lý đơn hàng
- **Payment Gateway Integration**: Sẵn sàng tích hợp VNPay, Momo
- **Transaction History**: Lịch sử giao dịch chi tiết

#### 5. **AI Assistant - Chatbot Thông Minh**
- **Contextual Understanding**: Hiểu ngữ cảnh học tập của sinh viên
- **Course Recommendations**: Gợi ý khóa học phù hợp
- **Q&A Support**: Trả lời câu hỏi về nội dung học tập
- **Career Guidance**: Tư vấn định hướng nghề nghiệp

###  Số Liệu Hệ Thống

| Metric | Value |
|--------|-------|
| **Microservices** | 10 services |
| **Database Tables** | ~150+ tables across all services |
| **API Endpoints** | ~200+ REST APIs |
| **Message Events** | ~50+ integration events |
| **External APIs** | 4 (OpenAI, Groq, Judge0, Cloudinary) |
| **Lines of Code** | ~100,000+ LOC |
| **Deployment Units** | 10 Docker containers |

---

## ️ Kiến Trúc Hệ Thống Chi Tiết

###  Design Patterns & Principles

Hệ thống áp dụng nhiều design patterns và principles hiện đại:

#### **Architectural Patterns**
1. **Microservices Architecture**
   - Service per Business Capability
   - Database per Service
   - API Gateway pattern (YARP)
   - Service Discovery (via Docker networking)

2. **Event-Driven Architecture (EDA)**
   - Publish-Subscribe pattern
   - Request-Response pattern với MassTransit
   - Event Sourcing với Marten
   - Outbox Pattern cho eventual consistency

3. **CQRS (Command Query Responsibility Segregation)**
   - Commands: Write operations qua EF Core → PostgreSQL
   - Queries: Read operations qua Marten → Document DB
   - MediatR làm mediator layer

4. **Clean Architecture / Onion Architecture**
   - Domain Layer (core business logic)
   - Application Layer (use cases, CQRS handlers)
   - Infrastructure Layer (database, external services)
   - Presentation Layer (API controllers)

#### **Design Principles**
- **SOLID Principles**: Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion
- **DRY (Don't Repeat Yourself)**: Shared libraries trong BuildingBlocks
- **Separation of Concerns**: Mỗi layer có trách nhiệm riêng biệt
- **Dependency Injection**: Built-in .NET DI container
- **Repository Pattern**: Abstraction cho data access
- **Unit of Work Pattern**: Transaction management
- **Template Method Pattern**: Standardized request handling workflow (xem chi tiết bên dưới)

#### **Template Method Pattern - Chuẩn Hóa Xử Lý Request**

Hệ thống sử dụng **Template Method Pattern** để chuẩn hóa luồng xử lý request trong tất cả các API controllers, đảm bảo tính nhất quán và giảm thiểu code duplication.

##### **Kiến Trúc Template Method**

```csharp
// Base Template Method trong ApiControllerHelper
public static async Task<TResponse> HandleRequest<TRequest, TResponse, TEntityResponse>(
    TRequest request,
    Logger logger,
    ModelStateDictionary modelState,
    Func<Task<TResponse>> exec,
    IIdentityService identityService,
    IdentityEntity identityEntity,
    IHttpContextAccessor httpContextAccessor,
    TResponse returnValue)
    where TResponse : AbstractApiResponse<TEntityResponse>
{
    // Step 1: Authentication & Identity Extraction
    var user = httpContextAccessor.HttpContext?.User;
    identityEntity = identityService.GetIdentity(user);
    
    var loggingUtil = new LoggingUtil(logger, identityEntity?.Email!);
    loggingUtil.StartLog(request);  // Logging bắt đầu
    
    // Step 2: Authentication Check
    if (identityEntity == null)
    {
        loggingUtil.FatalLog($"Authenticated, but information is missing.");
        returnValue.Success = false;
        returnValue.SetMessage(MessageId.E11006);
        loggingUtil.EndLog(returnValue);
        return returnValue;
    }
    
    try
    {
        // Step 3: Model Validation
        var detailErrors = AbstractFunction<TResponse, TEntityResponse>.ErrorCheck(modelState);
        if (detailErrors.Count > 0)
        {
            returnValue.Success = false;
            returnValue.SetMessage(MessageId.E10000);
            returnValue.DetailErrors = detailErrors;
            return returnValue;
        }
        
        // Step 4: Execute Business Logic (Hook Method)
        returnValue = await exec();  // Đây là "hook" - mỗi controller implement riêng
        return returnValue;
    }
    catch (Exception e)
    {
        // Step 5: Exception Handling
        loggingUtil.ErrorLog(e.Message);
        return AbstractFunction<TResponse, TEntityResponse>.GetReturnValue(returnValue, loggingUtil, e);
    }
    finally
    {
        // Step 6: Logging kết thúc
        loggingUtil.EndLog(returnValue);
    }
}
```

##### **Workflow Chuẩn (6 Bước Cố Định)**

```
┌─────────────────────────────────────────────────────────────┐
│              TEMPLATE METHOD WORKFLOW                        │
└─────────────────────────────────────────────────────────────┘
                           │
        ┌──────────────────┴──────────────────┐
        │                                     │
    ┌───▼────────────────────────┐           │
    │  Step 1: Authentication    │           │
    │  - Extract JWT token       │           │
    │  - Get user identity       │           │
    │  - Initialize logging      │           │
    └───┬────────────────────────┘           │
        │                                     │
    ┌───▼────────────────────────┐           │
    │  Step 2: Auth Check        │           │
    │  - Verify identity exists  │           │
    │  - Log fatal if missing    │           │
    │  - Return error response   │           │
    └───┬────────────────────────┘           │
        │                                     │
    ┌───▼────────────────────────┐           │
    │  Step 3: Model Validation  │           │
    │  - Check ModelState        │           │
    │  - Extract field errors    │           │
    │  - Format error messages   │           │
    └───┬────────────────────────┘           │
        │                                     │
    ┌───▼────────────────────────┐           │
    │  Step 4: Business Logic    │  ◄────── HOOK METHOD
    │  - Execute custom logic    │         (Controller-specific)
    │  - Call MediatR handler    │           │
    │  - Return business result  │           │
    └───┬────────────────────────┘           │
        │                                     │
    ┌───▼────────────────────────┐           │
    │  Step 5: Exception Handle  │           │
    │  - Catch all exceptions    │           │
    │  - Classify error types    │           │
    │  - AggregateException      │           │
    │  - PostgresException       │           │
    │  - InvalidOperationEx      │           │
    │  - Return error response   │           │
    └───┬────────────────────────┘           │
        │                                     │
    ┌───▼────────────────────────┐           │
    │  Step 6: Finalize Logging  │           │
    │  - Log response data       │           │
    │  - Log execution time      │           │
    │  - Close log context       │           │
    └────────────────────────────┘           │
                                             │
                      ◄─────────────────────┘
```

##### **Cách Sử Dụng Trong Controller**

**Example 1: Authenticated Request**
```csharp
[HttpGet("{teacherId:guid}")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public async Task<GetTeacherDetailResponse> GetById([FromRoute] Guid teacherId)
{
    var query = new GetTeacherDetailQuery(teacherId);
    
    // Template Method tự động xử lý:
    // - Authentication check
    // - Model validation
    // - Exception handling
    // - Logging
    return await ApiControllerHelper.HandleRequest<
        GetTeacherDetailQuery,                    // TRequest
        GetTeacherDetailResponse,                 // TResponse
        TeacherDetailDto                          // TEntityResponse
    >(
        query,                                    // Request data
        _logger,                                  // Logger instance
        ModelState,                               // Model validation state
        async () => await _sender.Send(query),    // HOOK: Business logic
        new GetTeacherDetailResponse()            // Response template
    );
}
```

**Example 2: Unauthenticated Request (Public API)**
```csharp
// Overload khác cho public APIs (không cần authentication)
public static async Task<TResponse> HandleRequest<TRequest, TResponse, TEntityResponse>(
    TRequest request,
    Logger logger,
    ModelStateDictionary modelState,
    Func<Task<TResponse>> exec,
    TResponse returnValue)
    where TResponse : AbstractApiResponse<TEntityResponse>
{
    var loggingUtil = new LoggingUtil(logger, "User do not authenticated");
    loggingUtil.StartLog(request);
    
    // Workflow tương tự nhưng bỏ qua authentication check
    // ...
}
```

##### **Exception Handling - Phân Loại Tự Động**

Template Method tự động phân loại và xử lý các loại exception:

```csharp
public static TResponse GetReturnValue(TResponse returnValue, LoggingUtil loggingUtil, Exception e)
{
    switch (e)
    {
        case AggregateException:
            // API connection error
            loggingUtil.FatalLog($"Report API connection error: {e.Message}");
            returnValue.SetMessage(MessageId.E99001);
            break;
            
        case InvalidOperationException when e.InnerException?.HResult == -2146233088:
            // Concurrency conflict (EF Core optimistic concurrency)
            loggingUtil.ErrorLog($"Concurrency conflict: {e.Message}");
            returnValue.SetMessage(MessageId.E99002);
            break;
            
        case PostgresException ex when ex.SqlState == "57014":
            // Database timeout
            loggingUtil.ErrorLog($"PostgresSQL timeout error: {ex.Message}");
            returnValue.SetMessage(MessageId.E99003);
            break;
            
        case PostgresException ex when ex.SqlState == "42P01":
            // Schema/view changed during execution
            loggingUtil.ErrorLog($"Schema/view changed: {ex.Message}");
            returnValue.SetMessage(MessageId.E99004);
            break;
            
        case PostgresException ex:
            // Other PostgreSQL errors
            loggingUtil.ErrorLog($"PostgresSQL system error: {ex.Message}");
            returnValue.SetMessage(MessageId.E99005);
            break;
            
        default:
            // Unhandled exception
            loggingUtil.ErrorLog($"Unhandled exception: {e.Message}");
            returnValue.SetMessage(MessageId.E99999);
            break;
    }
    
    returnValue.Success = false;
    loggingUtil.EndLog(returnValue);
    return returnValue;
}
```

##### **Model Validation - Tự Động Extract Errors**

```csharp
public static List<DetailError> ErrorCheck(ModelStateDictionary modelState)
{
    var detailErrorList = new List<DetailError>();
    
    if (modelState.IsValid) return detailErrorList;
    
    foreach (var entry in modelState)
    {
        var key = entry.Key;
        var modelStateEntity = entry.Value;
        
        if (modelStateEntity.ValidationState == ModelValidationState.Valid)
            continue;
        
        // Remove prefix "Value." from key
        var keyReplace = Regex.Replace(key, @"^Value\.", "");
        keyReplace = Regex.Replace(keyReplace, @"^Value\[\d+\]\.", "");
        
        // Get error message
        var errorMessage = string.Join("; ", 
            modelStateEntity.Errors.Select(e => e.ErrorMessage));
        
        var detailError = new DetailError();
        
        // Extract field name from key structure: object[index].property
        Match matchesKey;
        if ((matchesKey = new Regex(@"^(.*?)\[(\d+)\]\.(.*?)$").Match(keyReplace)).Success)
        {
            detailError.Field = matchesKey.Groups[1].Value; // List case
        }
        else
        {
            detailError.Field = keyReplace.Split('.').LastOrDefault(); // Single item
        }
        
        // Convert to lowercase
        detailError.Field = StringUtil.ToLowerCase(detailError.Field);
        detailError.ErrorMessage = errorMessage;
        detailError.MessageId = MessageId.E10000;
        detailErrorList.Add(detailError);
    }
    
    return detailErrorList;
}
```

##### **Lợi Ích Của Template Method Pattern**

1. **Consistency (Tính Nhất Quán)**:
   - Tất cả APIs follow cùng một workflow
   - Error handling đồng nhất
   - Logging format chuẩn hóa

2. **Code Reusability (Tái Sử Dụng Code)**:
   - Authentication logic: Viết 1 lần, dùng cho tất cả APIs
   - Validation logic: Centralized
   - Exception handling: Không cần repeat

3. **Maintainability (Dễ Bảo Trì)**:
   - Thay đổi workflow ở 1 nơi → Apply cho tất cả
   - Fix bug 1 lần → All controllers được fix
   - Add feature mới (e.g., rate limiting) → Easy to inject

4. **Separation of Concerns**:
   - Infrastructure concerns (auth, logging, validation) → Template
   - Business logic → Controllers/Handlers
   - Clear separation

5. **Testability**:
   - Mock template method dễ dàng
   - Test business logic riêng
   - Test infrastructure concerns riêng

6. **Developer Experience**:
   - Developers chỉ focus vào business logic
   - Không cần lo về boilerplate code
   - Onboarding mới nhanh hơn

##### **Sử Dụng Trong Các Services**

| Service | Controllers Using Template | Total Endpoints |
|---------|---------------------------|-----------------|
| AuthService | 100% | ~15 endpoints |
| StudentService | 100% | ~25 endpoints |
| QuizService | 100% | ~35 endpoints |
| CourseService | 100% | ~30 endpoints |
| AiService | 100% | ~20 endpoints |
| TeacherService | 100% | ~10 endpoints |
| PaymentService | 100% | ~15 endpoints |
| UtilityService | 100% | ~8 endpoints |

**Total**: ~158 endpoints sử dụng Template Method Pattern

### Kiến Trúc Tổng Quan (Chi Tiết)

```
                          ┌─────────────────────────────────────┐
                          │         CLIENT LAYER                │
                          │  (Web App, Mobile App, Admin Panel) │
                          └──────────────┬──────────────────────┘
                                         │ HTTPS
                                         │
                  ┌──────────────────────▼────────────────────────┐
                  │      YARP REVERSE PROXY (API GATEWAY)         │
                  │  - Authentication Middleware (JWT/OAuth)      │
                  │  - Authorization Middleware (Role-based)      │
                  │  - Rate Limiting & Throttling                 │
                  │  - CORS Policy Management                     │
                  │  - Request/Response Logging                   │
                  │  - Swagger Aggregation                        │
                  │  Port: 8080 | Path: /                         │
                  └───┬────┬────┬────┬────┬────┬────┬────┬───┬───┘
                      │    │    │    │    │    │    │    │   │
        ┌─────────────┘    │    │    │    │    │    │    │   │
        │                  │    │    │    │    │    │    │   │
┌───────▼─────────┐ ┌──────▼───────┐ ┌──────▼────────┐ ┌──┴────┐
│   AuthService   │ │StudentService│ │  QuizService  │ │  ...  │
│   Port: 7001    │ │ Port: 7002   │ │  Port: 7003   │ │       │
│   Path: /auth   │ │Path: /student│ │  Path: /quiz  │ │       │
│                 │ │              │ │               │ │       │
│ ┌─────────────┐ │ │┌───────────┐ │ │┌────────────┐ │ │       │
│ │Presentation │ │ ││Controllers│ │ ││Controllers │ │ │       │
│ │  (API)      │ │ ││   Layer   │ │ ││   Layer    │ │ │       │
│ └──────┬──────┘ │ │└─────┬─────┘ │ │└──────┬─────┘ │ │       │
│        │        │ │      │       │ │       │       │ │       │
│ ┌──────▼──────┐ │ │┌─────▼─────┐ │ │┌──────▼─────┐ │ │       │
│ │Application  │ │ ││Application│ │ ││Application │ │ │       │
│ │   Layer     │ │ ││   Layer   │ │ ││   Layer    │ │ │       │
│ │- Commands   │ │ ││- CQRS     │ │ ││- Handlers  │ │ │       │
│ │- Queries    │ │ ││- Handlers │ │ ││- Services  │ │ │       │
│ │- Validators │ │ ││- Services │ │ ││- DTOs      │ │ │       │
│ └──────┬──────┘ │ │└─────┬─────┘ │ │└──────┬─────┘ │ │       │
│        │        │ │      │       │ │       │       │ │       │
│ ┌──────▼──────┐ │ │┌─────▼─────┐ │ │┌──────▼─────┐ │ │       │
│ │  Domain     │ │ ││  Domain   │ │ ││  Domain    │ │ │       │
│ │   Layer     │ │ ││  Layer    │ │ ││  Layer     │ │ │       │
│ │- Entities   │ │ ││- Entities │ │ ││- WriteModel│ │ │       │
│ │- ValueObjs  │ │ ││- Rules    │ │ ││- ReadModel │ │ │       │
│ │- Events     │ │ ││- Events   │ │ ││- Events    │ │ │       │
│ └──────┬──────┘ │ │└─────┬─────┘ │ │└──────┬─────┘ │ │       │
│        │        │ │      │       │ │       │       │ │       │
│ ┌──────▼──────┐ │ │┌─────▼─────┐ │ │┌──────▼─────┐ │ │       │
│ │Infrastructure│ │││Infrastructure││││Infrastructure││ │       │
│ │   Layer     │ │ ││   Layer   │ │ ││   Layer    │ │ │       │
│ │- DbContext  │ │ ││- Repos    │ │ ││- Services  │ │ │       │
│ │- Repos      │ │ ││- EF Core  │ │ ││- EF + Marten│ │ │       │
│ │- Migrations │ │ ││- Cache    │ │ ││- Judge0 API│ │ │       │
│ └──────┬──────┘ │ │└─────┬─────┘ │ │└──────┬─────┘ │ │       │
│        │        │ │      │       │ │       │       │ │       │
└────────┼────────┘ └──────┼───────┘ └───────┼───────┘ └───┬───┘
         │                 │                 │             │
         └─────────────────┴─────────────────┴─────────────┘
                                    │
         ┌──────────────────────────┼──────────────────────────┐
         │                          │                          │
    ┌────▼─────┐            ┌──────▼──────┐          ┌───────▼──────┐
    │PostgreSQL│            │  RabbitMQ   │          │    Redis     │
    │ Database │            │Message Queue│          │    Cache     │
    │          │            │             │          │              │
    │Write Model│           │- Exchanges  │          │- Session     │
    │(EF Core) │            │- Queues     │          │- Distributed │
    │          │            │- Bindings   │          │  Cache       │
    │Read Model│            │- DLQ        │          │- Rate Limit  │
    │(Marten)  │            │             │          │  Counter     │
    └──────────┘            └─────────────┘          └──────────────┘
         │
         │
    ┌────▼─────┐
    │ Document │
    │  Store   │
    │ (Marten) │
    │          │
    │- Event   │
    │  Sourcing│
    │- JSONB   │
    │  Storage │
    └──────────┘


External Services Layer:
┌──────────────────────────────────────────────────────────────┐
│                    EXTERNAL APIs                              │
│                                                               │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐            │
│  │  OpenAI    │  │   Judge0   │  │ Cloudinary │            │
│  │  API       │  │Code Executor│  │Media Upload│            │
│  │ (GPT-4o)   │  │            │  │            │            │
│  └────────────┘  └────────────┘  └────────────┘            │
│                                                               │
│  ┌────────────┐  ┌────────────┐                             │
│  │  Groq AI   │  │  Google    │                             │
│  │Voice-to-Text│  │  OAuth     │                             │
│  └────────────┘  └────────────┘                             │
└──────────────────────────────────────────────────────────────┘
```

###  Communication Patterns (Chi Tiết)

#### 1. **Synchronous Communication (REST)**
```
Client → Gateway → Service
  ↓         ↓         ↓
[HTTPS]  [HTTP]   [Process]
  ↓         ↓         ↓
Response ← Response ← Result
```

**Use Cases**:
- Client queries: Lấy dữ liệu để hiển thị
- CRUD operations: Tạo, đọc, cập nhật, xóa
- Real-time responses: Cần kết quả ngay lập tức

#### 2. **Asynchronous Communication (Message Bus)**

**a) Request-Response Pattern với MassTransit**
```
QuizService                          StudentService
    │                                     │
    │─── Request(StudentTranscriptSelectEvent) ───→│
    │                                     │
    │                               [Query DB]
    │                                     │
    │←── Response(StudentTranscriptSelectEventResponse) ───│
    │                                     │
[Continue Processing]
```

**b) Publish-Subscribe Pattern**
```
CourseService                    NotificationService
    │                                  │
    │─── Publish(CoursePublishedEvent) ───→ RabbitMQ ───→│
    │                                  │
[Continue]                       [Send Notification]
```

**c) Outbox Pattern (Guaranteed Delivery)**
```
Service Transaction:
1. [BEGIN TRANSACTION]
2. Save Entity to DB
3. Save OutboxMessage to DB
4. [COMMIT TRANSACTION]

Background Worker (OutboxPublisher):
1. Query OutboxMessages (not published)
2. Publish to RabbitMQ
3. Mark as Published
4. Retry if failed (exponential backoff)
```

#### 3. **Service-to-Service Communication Matrix**

| From Service | To Service | Pattern | Purpose |
|--------------|------------|---------|---------|
| QuizService | StudentService | Request-Response | Get student transcript |
| QuizService | CourseService | Publish | Create learning path |
| QuizService | AiService | Request-Response | Analyze interest survey |
| CourseService | AiService | Request-Response | Generate course recommendations |
| StudentService | AiService | Request-Response | AI course suggestions |
| PaymentService | CourseService | Publish | Enrollment after payment |
| CourseService | NotificationService | Publish | Course published |
| QuizService | NotificationService | Publish | Test completed |
| All Services | AuthService | REST | Token validation |

### Công Nghệ Sử Dụng

#### Backend Framework & Libraries
- **.NET 9.0**: Framework chính
- **ASP.NET Core**: Web API
- **Entity Framework Core 9.0**: ORM cho PostgreSQL
- **Marten 8.5**: Document Database (MongoDB-style) trên PostgreSQL
- **MediatR**: CQRS và Mediator pattern
- **MassTransit**: Message Bus abstraction
- **RabbitMQ**: Message Broker
- **OpenIddict 7.0**: OAuth 2.0/OpenID Connect authentication
- **YARP (Yet Another Reverse Proxy)**: API Gateway
- **Mapster**: Object mapping
- **FluentValidation**: Validation
- **NLog**: Logging

#### Database & Caching
- **PostgreSQL**: Primary database (Write Model)
- **Marten**: Read Model (Event Sourcing & Document DB)
- **Redis**: Distributed caching
- **Stack Exchange Redis**: Redis client

#### AI & External Services
- **OpenAI API (GPT-4o-mini)**: AI chatbot và recommendations
- **Groq API**: Voice-to-text
- **Judge0 API**: Code evaluation cho practice tests
- **Cloudinary**: Upload và quản lý media (hình ảnh, video)

---

##  Microservices Chi Tiết

### 1. **AuthService** (Port 7001)
**Chức năng**: Xác thực và phân quyền người dùng

**Tính năng**:
- Đăng ký, đăng nhập (Email/Password, Google OAuth)
- Quản lý token (Access Token, Refresh Token)
- Phân quyền dựa trên vai trò (Student, Teacher, Admin)
- Quản lý tài khoản và thông tin người dùng
- Background job: Role seeder, Outbox message publisher

**Database**: `AuthServiceDB`

**Key Controllers**:
- `Accounts/`: Quản lý tài khoản
- `Auths/`: Xác thực và token

---

### 2. **StudentService** (Port 7002)
**Chức năng**: Quản lý thông tin và hoạt động của sinh viên

**Tính năng**:
- Quản lý hồ sơ sinh viên (thông tin cá nhân, chuyên ngành, học kỳ)
- Quản lý bảng điểm (transcript)
- Lưu trữ công nghệ/ngôn ngữ quan tâm
- Quản lý mục tiêu học tập (learning goals)
- Lộ trình học tập cá nhân (learning paths)
- Dashboard: thống kê tiến độ học tập
- Course suggestions: gợi ý khóa học dựa trên AI
- User behaviour tracking: theo dõi hành vi học tập

**Database**: `StudentServiceDB`

**Key Controllers**:
- `StudentController.cs`: CRUD sinh viên
- `LearningGoalController.cs`: Mục tiêu học tập
- `LearningPathsController.cs`: Lộ trình học tập
- `TechnologyController.cs`: Công nghệ quan tâm
- `CourseSuggestionsController.cs`: Gợi ý khóa học AI
- `StudentDashboardsController.cs`: Dashboard
- `UserBehaviourController.cs`: Tracking hành vi
- `AdminController.cs`: Quản trị
- `AiQuizEvaluateController.cs`: Đánh giá bài quiz bằng AI

**Message Events**:
- `StudentInformationSelectsEvent`: Lấy thông tin sinh viên
- `StudentTranscriptSelectEvent`: Lấy bảng điểm
- `CourseSuggestionInsertEvent`: Tạo gợi ý khóa học

---

### 3. **QuizService** (Port 7003)
**Chức năng**: Quản lý bài test, quiz, survey và đánh giá năng lực

**Tính năng**:
- **Quiz Management**: Tạo, chỉnh sửa, xóa quiz với nhiều loại câu hỏi (single choice, multiple choice)
- **Test System**: Bài kiểm tra đánh giá đầu vào (placement test)
- **Survey System**: Khảo sát thói quen học tập (HABIT) và sở thích (INTEREST)
- **Practice Test**: Bài tập lập trình tự luận (coding challenges)
- **Student Test Submission**: Submit bài test với logic phức tạp:
  - Tính toán level sinh viên (1-3) dựa trên quiz (60%), practice test (40%), transcript (20%)
  - Tính điểm năng lực (AbilityMarks) cho từng môn học
  - Kết hợp điểm quiz và bảng điểm (60% transcript + 40% quiz)
  - Xác định các năng lực cần cải thiện (< 70 điểm)
  - Tích hợp với Judge0 để chấm code
- **Learning Path Creation**: Tạo lộ trình học tập dựa trên:
  - Kết quả test và survey
  - Bảng điểm sinh viên
  - Mục tiêu học tập
  - AI analysis
- **Course Quiz**: Quiz cho từng khóa học
- **External Quiz**: Import quiz từ nguồn bên ngoài

**Database**: `QuizServiceDB`

**Key Controllers**:
- `StudentTestController.cs`: Submit và quản lý bài test
- `StudentSurveyController.cs`: Khảo sát sinh viên
- `QuizController.cs`: Quản lý quiz
- `TestController.cs`: Quản lý test
- `SurveyController.cs`: Quản lý survey
- `QuestionController.cs`: Quản lý câu hỏi
- `PracticeTestController.cs`: Bài tập lập trình
- `CourseQuizController.cs`: Quiz cho khóa học
- `LearningPathController.cs`: Lộ trình học tập
- `ExternalQuizController.cs`: Import quiz
- `AdminController.cs`: Quản trị

**Key Services**:
- `StudentTestService`: Xử lý logic submit test phức tạp
- `StudentSurveyService`: Xử lý khảo sát
- `LearningPathService`: Tạo lộ trình học tập với AI
- `PracticeTestService`: Tích hợp Judge0 API

**Message Events**:
- `QuizSelectEvent`: Lấy thông tin quiz
- `StudentTestInsertEvent`: Tạo student test
- `LearningPathCreateEvent`: Tạo learning path

**Đặc biệt**:
- Logic tính level rất phức tạp với nhiều công thức kết hợp
- Sử dụng AI để phân tích sở thích và đề xuất mục tiêu học tập
- Tích hợp Judge0 để chấm bài code tự động

---

### 4. **CourseService** (Port 7005)
**Chức năng**: Quản lý khóa học, module, bài học

**Tính năng**:
- Quản lý khóa học (CRUD, publish/unpublish)
- Quản lý module (chapters)
- Quản lý bài học (lessons) với nhiều loại nội dung
- Quản lý syllabus (đề cương)
- Quiz và bài kiểm tra cho khóa học
- Theo dõi tiến độ học tập (progress tracking)
- Comment và thảo luận
- Course wishlist
- Lesson notes
- Module discussion
- Tích hợp AI để tạo lộ trình học tập dựa trên:
  - Kết quả test/survey từ QuizService
  - Thông tin sinh viên từ StudentService
  - Phân tích năng lực và điểm yếu

**Database**: `CourseServiceDB`

**Key Controllers**:
- `CoursesController.cs`: CRUD khóa học
- `ModulesController.cs`: Quản lý module
- `LessonController.cs`: Quản lý bài học
- `SyllabusController.cs`: Đề cương
- `TestsController.cs`: Bài kiểm tra
- `StudentLessonProgressController.cs`: Tiến độ
- `CourseCommentsController.cs`: Comment
- `CourseWishlistController.cs`: Wishlist
- `LessonNotesController.cs`: Ghi chú
- `ModuleDiscussionCommentsController.cs`: Thảo luận

**Message Events**:
- `CourseSelectEvent`: Lấy thông tin khóa học
- `CourseMajorSemesterSelectEvent`: Lấy thông tin chuyên ngành/học kỳ
- `SubjectCodeSelectEvent`: Lấy danh sách mã môn học
- `CoreSubjectSelectEvent`: Lấy danh sách môn học nền tảng
- `InsertLearningPathEvent`: Tạo lộ trình học tập (consumer)

---

### 5. **AiService** (Port 7006)
**Chức năng**: Tích hợp AI và xử lý thông minh

**Tính năng**:
- **AI Chatbot**: Trò chuyện với AI để hỗ trợ học tập
- **AI Recommendations**: Đề xuất khóa học/tài liệu thông minh
- **AI Quiz Evaluation**: Đánh giá câu trả lời tự luận
- **AI Summary**: Tóm tắt nội dung bài học/tài liệu
- **AI Transcript**: Chuyển đổi giọng nói thành văn bản (voice-to-text)
- **Interest Survey Analysis**: Phân tích khảo sát sở thích để đề xuất mục tiêu học tập
- **Learning Path AI**: Sử dụng AI để tạo lộ trình học tập tối ưu
- Tích hợp OpenAI GPT-4o-mini
- Tích hợp Groq AI cho voice-to-text
- Upload media với Cloudinary

**Database**: `AIServiceDB`

**Key Controllers**:
- `AIChatBotsController.cs`: Chatbot
- `AiRecommendController.cs`: Gợi ý thông minh
- `AiQuizEvaluateController.cs`: Đánh giá quiz
- `AiSummarysController.cs`: Tóm tắt nội dung
- `AiTranscriptController.cs`: Voice-to-text

**Message Events**:
- `StudentInterestSurveyAnalysisEvent`: Phân tích khảo sát sở thích (consumer)
- `AiRecommendationEvent`: Tạo gợi ý AI

**External APIs**:
- OpenAI API (GPT-4o-mini, text-embedding-3-small)
- Groq AI API
- Cloudinary API

---

### 6. **TeacherService** (Port 7004)
**Chức năng**: Quản lý thông tin giảng viên

**Tính năng**:
- Quản lý hồ sơ giảng viên
- Quản lý chuyên môn
- Liên kết với khóa học
- Đánh giá và rating

**Database**: `TeacherServiceDB`

**Key Controllers**:
- `TeachersController.cs`: CRUD giảng viên

---

### 7. **PaymentService** (Port 7007)
**Chức năng**: Xử lý thanh toán và đơn hàng

**Tính năng**:
- Giỏ hàng (cart)
- Quản lý đơn hàng (orders)
- Xử lý thanh toán
- Lịch sử giao dịch
- Tích hợp payment gateway (dự kiến: VNPay, Momo)

**Database**: `PaymentServiceDB`

**Key Controllers**:
- `CartController.cs`: Giỏ hàng
- `OrderController.cs`: Đơn hàng
- `PaymentController.cs`: Thanh toán

**Message Events**:
- `PaymentProcessEvent`: Xử lý thanh toán
- `OrderCreateEvent`: Tạo đơn hàng

---

### 8. **NotificationService** (Port 7008)
**Chức năng**: Gửi thông báo cho người dùng

**Tính năng**:
- Push notification (dự kiến)
- Email notification (dự kiến)
- In-app notification
- Notification preferences
- Event-driven: Lắng nghe các event từ services khác và gửi thông báo tương ứng

**Database**: `NotificationServiceDB`

**Message Events** (Consumers):
- `CoursePublishedEvent`: Thông báo khóa học mới
- `OrderCompletedEvent`: Thông báo đơn hàng thành công
- `TestResultEvent`: Thông báo kết quả test
- `CommentReplyEvent`: Thông báo reply comment

---

### 9. **UtilityService** (Port 7010)
**Chức năng**: Các tiện ích chung

**Tính năng**:
- Upload files (documents, PDFs)
- Upload videos
- File management
- Media processing
- Tích hợp Cloudinary

**Database**: `UtilityServiceDB`

**Key Controllers**:
- `UploadFiles/`: Upload file
- `UploadVideos/`: Upload video

---

### 10. **ReverseProxy** (YARP Gateway - Port 8080)
**Chức năng**: API Gateway và routing

**Tính năng**:
- Route requests đến các microservices
- Authentication & Authorization middleware
- CORS configuration
- Rate limiting
- Load balancing
- Swagger aggregation (tổng hợp API docs từ tất cả services)
- Role-based authorization với custom middleware

**Path Mappings**:
- `/auth/*` → AuthService (7001)
- `/student/*` → StudentService (7002)
- `/quiz/*` → QuizService (7003)
- `/teacher/*` → TeacherService (7004)
- `/course/*` → CourseService (7005)
- `/ai/*` → AiService (7006)
- `/payment/*` → PaymentService (7007)
- `/notification/*` → NotificationService (7008)
- `/utility/*` → UtilityService (7010)

**Key Features**:
- OpenIddict integration cho authentication
- Custom `RoleAuthorizationMiddleware` để kiểm tra quyền
- Swagger UI tổng hợp tại `/swagger`

---

##  BuildingBlocks (Shared Libraries)

### BuildingBlocks
**Chức năng**: Shared components cho tất cả services

**Bao gồm**:
- **CQRS**: Command/Query base classes
- **Behaviors**: MediatR pipeline behaviors (Validation, Logging)
- **Pagination**: Pagination helpers

### BuildingBlocks.Messaging
**Chức năng**: Message Bus abstraction và integration events

**Bao gồm**:
- **IntegrationEvent**: Base class cho tất cả events
- **Events**: Events cho từng service (AIService, AuthService, CourseService, QuizService, StudentService, TeacherService, PaymentService, UtilityService)
- **Extensions**: MassTransit configuration helpers

**Key Events**:
- `StudentInformationSelectsEvent`: Lấy thông tin sinh viên
- `StudentTranscriptSelectEvent`: Lấy bảng điểm
- `QuizSelectEvent`: Lấy quiz
- `CourseSelectEvent`: Lấy khóa học
- `InsertLearningPathEvent`: Tạo learning path
- `StudentInterestSurveyAnalysisEvent`: Phân tích khảo sát AI
- ... và nhiều events khác

---

## ️ Database Architecture

### Write Model (PostgreSQL)
Mỗi service có database riêng theo pattern **Database per Service**:
- `AuthServiceDB`
- `StudentServiceDB`
- `QuizServiceDB`
- `CourseServiceDB`
- `AIServiceDB`
- `TeacherServiceDB`
- `PaymentServiceDB`
- `NotificationServiceDB`
- `UtilityServiceDB`

### Read Model (Marten - Document Database)
- Sử dụng Marten để tạo read model tối ưu trên PostgreSQL
- Document-style storage (giống MongoDB)
- Hỗ trợ Event Sourcing pattern
- Tối ưu cho query phức tạp

### Caching (Redis)
- Distributed caching cho tất cả services
- Session management
- Temporary data storage
- Connection: `157.66.25.29:6379`

---

##  Message Bus Architecture

### RabbitMQ Configuration
- **Host**: `localhost` (local) hoặc `157.66.25.29` (production)
- **Username**: `admin`
- **Password**: `EduSmart@123`

### Communication Patterns

#### 1. **Request-Response Pattern**
Sử dụng MassTransit với RabbitMQ để giao tiếp đồng bộ giữa services:

```
StudentService → Request(StudentTranscriptSelectEvent) → StudentService
                           ← Response ←
```

#### 2. **Publish-Subscribe Pattern**
Event-driven communication cho các hành động bất đồng bộ:

```
QuizService → Publish(TestCompletedEvent) → RabbitMQ
                                              ↓
                            NotificationService (Subscribe)
```

#### 3. **Outbox Pattern**
Đảm bảo eventual consistency:
- Mỗi service có bảng `OutboxMessage`
- Background worker (OutboxPublisher) publish messages
- Đảm bảo không mất message khi có lỗi

---

##  Authentication & Authorization

### OpenIddict (OAuth 2.0 / OpenID Connect)
- **Grant Types**: 
  - Client Credentials (service-to-service)
  - Password (user login)
  - Refresh Token
- **Scopes**: Custom scopes cho từng service
- **Clients**: 
  - `service_client`: Internal service communication
  - Web/Mobile clients

### JWT Token
- **Issuer**: AuthService
- **Audience**: `service_client`
- **Claims**: UserId, Email, Role, FullName
- **Expiration**: Access Token (1 hour), Refresh Token (30 days)

### Role-Based Authorization
- **Roles**: Student, Teacher, Admin
- Gateway middleware kiểm tra role trước khi route request
- Mỗi endpoint có attribute `[Authorize(Roles = "...")]`

---

##  Deployment

### Docker Compose
Tất cả services được containerized và deploy với Docker Compose:

```bash
docker-compose up -d
```

### Services Ports
| Service | Port | Path Base |
|---------|------|-----------|
| ReverseProxy | 8080 | `/` |
| AuthService | 7001 | `/auth` |
| StudentService | 7002 | `/student` |
| QuizService | 7003 | `/quiz` |
| TeacherService | 7004 | `/teacher` |
| CourseService | 7005 | `/course` |
| AiService | 7006 | `/ai` |
| PaymentService | 7007 | `/payment` |
| NotificationService | 7008 | `/notification` |
| UtilityService | 7010 | `/utility` |

### Environment Variables
Tất cả configuration được quản lý qua file `.env`:
- Database connections
- RabbitMQ configuration
- Redis connection
- API keys (OpenAI, Judge0, Cloudinary, Google OAuth)
- Service URLs
- Encryption keys

### Logging
- Mỗi service log vào folder riêng: `/root/apps/EduSmart-BackEnd/logs/{service-name}/`
- Sử dụng NLog
- Log levels: Debug, Info, Warning, Error

---

##  API Testing

### Swagger UI
Mỗi service có Swagger UI riêng:
- AuthService: `http://localhost:7001/swagger`
- StudentService: `http://localhost:7002/swagger`
- QuizService: `http://localhost:7003/swagger`
- ... (tương tự cho các service khác)

Gateway tổng hợp tất cả:
- Gateway: `http://localhost:8080/swagger`

### HTTP Files
Mỗi service có file `.http` để test API:
- `AuthService.API.http`
- `StudentService.API.http`
- `QuizService.API.http`
- ... (tương tự)

---

##  Data Flow Examples

### 1. Student Submit Test Flow

```
1. Student → Gateway → QuizService: Submit test answers
2. QuizService:
   a. Validate answers
   b. Calculate quiz score
   c. Submit practice tests to Judge0 API
   d. Request transcript từ StudentService
   e. Calculate student level (quiz 60% + practice 40% + transcript 20%)
   f. Calculate ability marks for each subject
   g. Determine abilities need improvement (< 70 points)
   h. Get latest HABIT & INTEREST surveys
   i. If no learning goal → Request AI analysis from AiService
3. QuizService → CourseService: Create learning path event
4. CourseService:
   a. Use AI to analyze student context
   b. Create personalized learning path with courses
   c. Match courses with student level and goals
5. QuizService → Student: Return test result & learning path ID
```

### 2. Learning Path Creation Flow

```
1. CourseService receives InsertLearningPathEvent from QuizService
2. CourseService:
   a. Collect context:
      - Student level (1-3)
      - Subject marks (từ bảng điểm)
      - Ability marks (từ test results)
      - Course improve list (môn cần cải thiện)
      - Ability improve list (năng lực < 70)
      - Learning goal & technologies
      - Limit time (giờ học/tuần)
   b. Call AI Service với context
   c. AI returns course recommendations
   d. Create learning path with modules & courses
   e. Set priorities and schedules
   f. Save to database
3. Return learning path ID
```

### 3. AI Course Suggestion Flow

```
1. Student views course → StudentService logs behaviour
2. StudentService:
   a. Track user behaviour (view, click, time spent)
   b. Collect student profile, transcript, interests
3. Periodically:
   a. StudentService → AiService: Request suggestions
   b. AiService analyzes:
      - Viewing patterns
      - Course completion rate
      - Transcript & grades
      - Learning goals
      - Technologies of interest
   c. AI generates personalized suggestions
   d. Return top N courses with scores
4. StudentService saves suggestions
5. Display on student dashboard
```

---

##  Key Features Deep Dive

### 1. Intelligent Student Level Assessment

**Algorithm**:
```
Base Level = Quiz Score (1-3)

If has Practice Test:
  Practice Level = Easy Score * 0.3 + Medium * 0.5 + Hard * 0.7
  Base Level = Quiz Level * 0.6 + Practice Level * 0.4

If has Matching Transcript:
  Transcript Level = convert(Average Grade) → 1-3
  Final Level = Base Level * 0.8 + Transcript Level * 0.2

Ensure: 1 ≤ Final Level ≤ 3
```

**Rationale**:
- Quiz (60%): Đánh giá kiến thức lý thuyết hiện tại
- Practice Test (40%): Đánh giá kỹ năng thực hành coding
- Transcript (20%): Tham khảo kiến thức đã học trước đó

### 2. Ability Marks Calculation

**For Each Subject**:
```
Quiz Score = (Correct Answers / Total Questions) * 100

If has Transcript for Subject:
  Transcript Score = Grade * 10  // Convert 0-10 to 0-100
  Final Score = Transcript * 0.6 + Quiz * 0.4
Else If answered Quiz:
  Final Score = Quiz Score
Else:
  Final Score = 0

Ability Mark = {Subject Name, Final Score}
```

**For Practice Tests**:
```
Practice Score = (Passed Test Cases / Total Test Cases) * 100
Ability Mark = {Problem Title + Difficulty, Practice Score}
```

**Improvement Threshold**:
- Abilities with score < 70 → Need improvement
- Added to `AbilityImprove` list for AI to prioritize

### 3. Course Improvement Logic

**Based on OtherQuestionAnswerCodes**:
```
GRADE_5_TO_7_COURSE:
  → Courses for subjects with grades 5-7
  → Level: Beginner
  → Purpose: Học lại nền tảng

GRADE_7_TO_8_COURSE:
  → Courses for subjects with grades 7-8
  → Level: Intermediate
  → Purpose: Nâng cao kiến thức

GRADE_8_TO_9_COURSE:
  → Courses for subjects with grades 8-9
  → Level: Advanced
  → Purpose: Chuyên sâu

GRADE_5_TO_7_EVALUATION:
  → Add to evaluation list
  → AI will create assessment quizzes

GRADE_7_TO_8_EVALUATION:
  → Add to evaluation list
  → AI will create assessment quizzes
```

### 4. AI Interest Analysis

**Input**: INTEREST survey answers
```
Questions: [
  {
    QuestionText: "Bạn thích làm việc với công nghệ nào?",
    StudentAnswers: ["AI/Machine Learning", "Backend Development"]
  },
  ...
]
```

**AI Processing**:
- Analyze survey answers using GPT-4o-mini
- Extract learning preferences
- Match with available learning goals
- Return: `{ LearningGoal: "AI Engineer" }`

**Output**: Personalized learning goal recommendation

---

## 🛠️ Development Guide

### Prerequisites
- .NET 9.0 SDK
- Docker & Docker Compose
- PostgreSQL 16
- Redis
- RabbitMQ
- IDE: JetBrains Rider / Visual Studio 2022 / VS Code

### Clone Repository
```bash
git clone <repository-url>
cd EduSmart-BackEnd
```

### Setup Environment
1. Copy `.env.example` to `.env` (if exists)
2. Configure database connections
3. Add API keys (OpenAI, Judge0, Cloudinary)

### Run Locally (Docker)
```bash
docker-compose up -d
```

### Run Locally (Development)
```bash
# Terminal 1: AuthService
cd Services/AuthService/AuthService.API
dotnet run

# Terminal 2: StudentService
cd Services/StudentService/StudentService.API
dotnet run

# Terminal 3: QuizService
cd Services/QuizService/QuizService.API
dotnet run

# ... (repeat for other services)

# Gateway
cd Gateways/ReverseProxy
dotnet run
```

### Database Migration
```bash
cd Services/{ServiceName}/{ServiceName}.Infrastructure
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Testing
```bash
# Run all tests
dotnet test

# Run specific service tests
cd Services/{ServiceName}/{ServiceName}.Tests
dotnet test
```

---

##  Code Structure

### Clean Architecture Layers

Mỗi service tuân theo **Clean Architecture**:

```
ServiceName/
├── ServiceName.API/           # Presentation Layer
│   ├── Controllers/           # API Endpoints
│   ├── Extensions/            # DI Configuration
│   └── Program.cs             # Entry Point
├── ServiceName.Application/   # Application Layer
│   ├── Commands/              # CQRS Commands
│   ├── Queries/               # CQRS Queries
│   ├── DTOs/                  # Data Transfer Objects
│   ├── Interfaces/            # Service Interfaces
│   └── Validators/            # FluentValidation
├── ServiceName.Domain/        # Domain Layer
│   ├── Entities/              # Domain Entities
│   ├── ValueObjects/          # Value Objects
│   ├── Enums/                 # Enumerations
│   └── Events/                # Domain Events
└── ServiceName.Infrastructure/ # Infrastructure Layer
    ├── Data/                  # DbContext, Configurations
    ├── Repositories/          # Repository Implementations
    ├── Implements/            # Service Implementations
    └── Migrations/            # EF Migrations
```

### CQRS Pattern

**Commands** (Write Operations):
```csharp
public class CreateStudentCommand : IRequest<StudentResponse>
{
    public string FullName { get; set; }
    public string Email { get; set; }
}

public class CreateStudentCommandHandler : IRequestHandler<CreateStudentCommand, StudentResponse>
{
    public async Task<StudentResponse> Handle(CreateStudentCommand request, CancellationToken cancellationToken)
    {
        // Business logic
    }
}
```

**Queries** (Read Operations):
```csharp
public class GetStudentQuery : IRequest<StudentResponse>
{
    public Guid StudentId { get; set; }
}

public class GetStudentQueryHandler : IRequestHandler<GetStudentQuery, StudentResponse>
{
    public async Task<StudentResponse> Handle(GetStudentQuery request, CancellationToken cancellationToken)
    {
        // Read logic
    }
}
```

### Repository Pattern

```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<List<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
}
```

---

##  Configuration Examples

### appsettings.json (Service)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database={ServiceName}DB;Username=postgres;Password=postgres"
  },
  "Authentication": {
    "Authority": "http://localhost:7001",
    "Audience": "service_client"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "admin",
    "Password": "EduSmart@123"
  },
  "Redis": {
    "Configuration": "localhost:6379"
  }
}
```

### Gateway Configuration (YARP)
```csharp
// RouteConfiguration.cs
public static RouteConfig[] GetRoutes()
{
    return new[]
    {
        new RouteConfig
        {
            RouteId = "student-route",
            ClusterId = "student-cluster",
            Match = new RouteMatch { Path = "/student/{**catch-all}" }
        },
        // ... other routes
    };
}

// ClusterConfiguration.cs
public static ClusterConfig[] GetClusters()
{
    return new[]
    {
        new ClusterConfig
        {
            ClusterId = "student-cluster",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                { "destination1", new DestinationConfig { Address = "http://studentservice.api" } }
            }
        },
        // ... other clusters
    };
}
```

---

##  Best Practices

### 1. **Separation of Concerns**
- Mỗi service chịu trách nhiệm cho một domain cụ thể
- Không truy cập trực tiếp database của service khác
- Giao tiếp qua Message Bus hoặc REST API

### 2. **Eventual Consistency**
- Sử dụng Outbox Pattern
- Event-driven architecture
- Chấp nhận độ trễ nhỏ để đổi lấy scalability

### 3. **Idempotency**
- Tất cả message handlers phải idempotent
- Sử dụng unique IDs để tránh xử lý trùng

### 4. **Error Handling**
- Global exception handler
- Structured logging với NLog
- Retry policies với MassTransit

### 5. **Security**
- JWT token cho authentication
- Role-based authorization
- Input validation với FluentValidation
- SQL injection prevention với EF Core
- Encryption cho sensitive data

### 6. **Performance**
- Redis caching
- Database indexing
- Lazy loading (where appropriate)
- Pagination cho large datasets
- Connection pooling

---

##  Troubleshooting

### Common Issues

#### 1. Database Connection Errors
```bash
# Check PostgreSQL is running
docker ps | grep postgres

# Test connection
psql -h localhost -U postgres -d StudentServiceDB
```

#### 2. RabbitMQ Connection Errors
```bash
# Check RabbitMQ is running
docker ps | grep rabbitmq

# Access RabbitMQ Management UI
http://localhost:15672
```

#### 3. Service Not Starting
```bash
# Check logs
docker logs {service-name}

# Or local logs
cat /root/apps/EduSmart-BackEnd/logs/{service-name}/log.txt
```

#### 4. JWT Token Invalid
- Check token expiration
- Verify `AUTH_SERVICE_URL` is correct
- Ensure AuthService is running
- Check client credentials

---

##  Contact & Support

- **Team**: EduSmart AI Development Team
- **Project**: Capstone Project
- **Version**: 1.0.0
- **Last Updated**: December 28, 2025

---

##  License

This project is proprietary software for educational purposes.

---

##  Academic Context

Đây là dự án Capstone (đồ án tốt nghiệp) xây dựng hệ thống học tập thông minh sử dụng AI để:
1. Đánh giá năng lực sinh viên
2. Tạo lộ trình học tập cá nhân hóa
3. Gợi ý khóa học phù hợp
4. Hỗ trợ học tập với AI chatbot
5. Theo dõi và cải thiện tiến độ học tập

Hệ thống sử dụng các công nghệ hiện đại và best practices trong phát triển phần mềm:
- Microservices Architecture
- Event-Driven Architecture
- CQRS Pattern
- Clean Architecture
- Domain-Driven Design
- AI/Machine Learning Integration

---

**Happy Coding!**

