using BaseService.Application.Interfaces.Commons;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Infrastructure.Identities;
using BaseService.Infrastructure.Logics;
using BaseService.Infrastructure.Repositories;
using StudentService.Application.Applications.LearningGoals.Commands;
using StudentService.Application.Applications.LearningGoals.Queries;
using StudentService.Application.Applications.Students.Commands.Inserts;
using StudentService.Application.Applications.Technologies.Commands;
using StudentService.Application.Applications.UserBehaviours.Commands;
using StudentService.Application.Interfaces;
using StudentService.Domain.ReadModels;
using StudentService.Domain.WriteModels;
using StudentService.Infrastructure.Implements;

namespace StudentService.API.Extensions;

public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositoryServices(this IServiceCollection services)
    {
        // Repository services
        services.AddScoped<ICommonLogic, CommonLogic>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICommandRepository<Student>, CommandRepository<Student>>();
        services.AddScoped<ICommandRepository<LearningGoal>, CommandRepository<LearningGoal>>();
        services.AddScoped<ICommandRepository<StudentTechnology>, CommandRepository<StudentTechnology>>();
        services.AddScoped<ICommandRepository<StudentLearningGoal>, CommandRepository<StudentLearningGoal>>();
        services.AddScoped<ICommandRepository<Technology>, CommandRepository<Technology>>();
        services.AddScoped<ICommandRepository<StudentOrientation>, CommandRepository<StudentOrientation>>();
        services.AddScoped<ICommandRepository<OutboxMessage>, CommandRepository<OutboxMessage>>();
        services.AddScoped<ICommandRepository<LearningPath>, CommandRepository<LearningPath>>();
        services.AddScoped<ICommandRepository<LearningPathMajor>, CommandRepository<LearningPathMajor>>();
        services.AddScoped<ICommandRepository<LearningPathCourse>, CommandRepository<LearningPathCourse>>();
        services.AddScoped<ICommandRepository<UserBehaviour>, CommandRepository<UserBehaviour>>();
        services.AddScoped<ICommandRepository<AiEvaluation>, CommandRepository<AiEvaluation>>();
        services.AddScoped<ICommandRepository<CourseSuggestion>, CommandRepository<CourseSuggestion>>();
        services.AddScoped<ICommandRepository<AiEvaluationImprovement>, CommandRepository<AiEvaluationImprovement>>();
        services.AddScoped<ICommandRepository<VwUserPlayvideoStreak>, CommandRepository<VwUserPlayvideoStreak>>();
        services.AddScoped<ICommandRepository<VwUserPlayvideoTimeSlot>, CommandRepository<VwUserPlayvideoTimeSlot>>();
        services.AddScoped<ICommandRepository<VwUserVideoActionsAgg>, CommandRepository<VwUserVideoActionsAgg>>();
		services.AddScoped<ICommandRepository<StudentTranscript>, CommandRepository<StudentTranscript>>();

        services.AddScoped<IQueryRepository<StudentCollection>, QueryRepository<StudentCollection>>();
        services.AddScoped<IQueryRepository<LearningGoalCollection>, QueryRepository<LearningGoalCollection>>();
        services.AddScoped<IQueryRepository<StudentLearningGoalCollection>, QueryRepository<StudentLearningGoalCollection>>();
        services.AddScoped<IQueryRepository<TechnologyCollection>, QueryRepository<TechnologyCollection>>();
        services.AddScoped<IQueryRepository<StudentOrientationCollection>, QueryRepository<StudentOrientationCollection>>();
        services.AddScoped<IQueryRepository<StudentTechnologyCollection>, QueryRepository<StudentTechnologyCollection>>();
        services.AddScoped<IQueryRepository<LearningPathCollection>, QueryRepository<LearningPathCollection>>();
        services.AddScoped<IQueryRepository<UserBehaviourCollection>, QueryRepository<UserBehaviourCollection>>();
        services.AddScoped<IQueryRepository<LearningPathCourseCollection>, QueryRepository<LearningPathCourseCollection>>();
        services.AddScoped<IQueryRepository<CourseSuggestionCollection>, QueryRepository<CourseSuggestionCollection>>();

        // Services
        services.AddScoped<IStudentService, Infrastructure.Implements.StudentService>();
        services.AddScoped<ILearningGoalService, LearningGoalService>();
        services.AddScoped<ITechnologyService, TechnologyService>();
        services.AddScoped<ILearningPathService, LearningPathService>();
        services.AddScoped<IUserBehaviourService, UserBehaviourService>();
        services.AddScoped<IAiQuizEvaluateStudentService, AiQuizEvaluateStudentService>();
        services.AddScoped<ICourseSuggestionService, CourseSuggestionService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAiEvaluationService, AiEvaluationService>();

        // MediatR configuration
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<StudentInsertCommandHandler>();
            cfg.RegisterServicesFromAssemblyContaining<LearningGoalInsertCommandHandler>();
            cfg.RegisterServicesFromAssemblyContaining<LearningGoalSelectsQueryHandler>();
            cfg.RegisterServicesFromAssemblyContaining<TechnologyInsertCommandHandler>();
            cfg.RegisterServicesFromAssemblyContaining<StudentMajorSemesterInsertCommandHandler>();
            cfg.RegisterServicesFromAssemblyContaining<TechnologySelectsQueryHandler>();
            cfg.RegisterServicesFromAssemblyContaining<UserBehaviourInsertCommandHandler>();
        });
        return services;
    }
}