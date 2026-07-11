// Skanly.Application/Features/Universities/Services/UniversityService.cs
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Skanly.Application.Common.Interfaces;
using Skanly.Application.Common.Models;
using Skanly.Application.Features.Universities.DTOs;
using Skanly.Application.Features.Universities.Interfaces;
using Skanly.Domain.Entities;

namespace Skanly.Application.Features.Universities.Services;

public class UniversityService : IUniversityService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateUniversityDto> _createValidator;
    private readonly IValidator<UpdateUniversityDto> _updateValidator;
    private readonly ILogger<UniversityService> _logger;

    public UniversityService(
        IUnitOfWork uow,
        IMapper mapper,
        IValidator<CreateUniversityDto> createValidator,
        IValidator<UpdateUniversityDto> updateValidator,
        ILogger<UniversityService> logger)
    {
        _uow = uow;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    public async Task<ServiceResult<PagedResult<UniversityDto>>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? searchTerm = null,
        bool? isActive = null,
        CancellationToken ct = default)
    {
        var (items, totalCount) = await _uow.Universities.GetPagedAsync(
            pageNumber: pageNumber,
            pageSize: pageSize,
            predicate: u =>
                (isActive == null || u.IsActive == isActive) &&
                (searchTerm == null ||
                 u.NameEn.ToLower().Contains(searchTerm.ToLower()) ||
                 u.NameAr.Contains(searchTerm)),
            orderBy: q => q.OrderBy(u => u.NameEn),
            ct: ct);

        var dtos = await EnrichWithStatsAsync(items, ct);

        return ServiceResult<PagedResult<UniversityDto>>.Success(
            PagedResult<UniversityDto>.Create(dtos, totalCount, pageNumber, pageSize));
    }

    // ── GetActiveListAsync ────────────────────────────────────────────────────

    public async Task<ServiceResult<IReadOnlyList<UniversityDto>>> GetActiveListAsync(
        CancellationToken ct = default)
    {
        var universities = await _uow.Universities.GetActiveAsync(ct);
        var dtos = universities
            .Select(u => _mapper.Map<UniversityDto>(u))
            .ToList();

        return ServiceResult<IReadOnlyList<UniversityDto>>.Success(dtos);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    public async Task<ServiceResult<UniversityDto>> GetByIdAsync(
        int universityId,
        CancellationToken ct = default)
    {
        var university = await _uow.Universities.GetByIdAsync(universityId, ct);

        if (university is null)
            return ServiceResult<UniversityDto>.Failure("University not found.");

        var dto = _mapper.Map<UniversityDto>(university);

        // Enrich with live stats
        dto = dto with
        {
            TotalProperties = await _uow.Properties.CountAsync(
                p => p.UniversityId == universityId && p.IsApproved, ct),
            TotalStudents = await _uow.Students.CountAsync(
                s => s.UniversityId == universityId, ct)
        };

        return ServiceResult<UniversityDto>.Success(dto);
    }

    // ── GetMostPopularAsync ───────────────────────────────────────────────────

    public async Task<ServiceResult<IReadOnlyList<UniversityDto>>> GetMostPopularAsync(
        int top = 5,
        CancellationToken ct = default)
    {
        var popular = await _uow.Universities.GetMostPopularAsync(top, ct);

        var dtos = popular
            .Select(x =>
            {
                var dto = _mapper.Map<UniversityDto>(x.University);
                return dto with { TotalProperties = x.PropertyCount };
            })
            .ToList();

        return ServiceResult<IReadOnlyList<UniversityDto>>.Success(dtos);
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    public async Task<ServiceResult<UniversityDto>> CreateAsync(
        CreateUniversityDto dto,
        CancellationToken ct = default)
    {
        // 1. Validate
        var validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ServiceResult<UniversityDto>.Failure(
                validation.Errors.Select(e => e.ErrorMessage).ToList());

        // 2. Check name uniqueness
        var exists = await _uow.Universities.ExistsAsync(
            u => u.NameEn.ToLower() == dto.NameEn.ToLower(), ct);

        if (exists)
            return ServiceResult<UniversityDto>.Failure(
                "A university with this English name already exists.");

        // 3. Map and persist
        var university = _mapper.Map<University>(dto);

        await _uow.Universities.AddAsync(university, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "University created: {NameEn} | Id: {Id}", university.NameEn, university.Id);

        return ServiceResult<UniversityDto>.Success(_mapper.Map<UniversityDto>(university));
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    public async Task<ServiceResult<UniversityDto>> UpdateAsync(
        UpdateUniversityDto dto,
        CancellationToken ct = default)
    {
        // 1. Validate
        var validation = await _updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ServiceResult<UniversityDto>.Failure(
                validation.Errors.Select(e => e.ErrorMessage).ToList());

        // 2. Load entity
        var university = await _uow.Universities.GetByIdAsync(dto.UniversityId, ct);
        if (university is null)
            return ServiceResult<UniversityDto>.Failure("University not found.");

        // 3. Check name uniqueness (exclude self)
        var nameConflict = await _uow.Universities.ExistsAsync(
            u => u.NameEn.ToLower() == dto.NameEn.ToLower() &&
                 u.Id != dto.UniversityId, ct);

        if (nameConflict)
            return ServiceResult<UniversityDto>.Failure(
                "Another university with this English name already exists.");

        // 4. Apply changes
        _mapper.Map(dto, university);
        _uow.Universities.Update(university);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "University updated: {NameEn} | Id: {Id}", university.NameEn, university.Id);

        return ServiceResult<UniversityDto>.Success(_mapper.Map<UniversityDto>(university));
    }

    // ── ToggleActiveAsync ─────────────────────────────────────────────────────

    public async Task<ServiceResult> ToggleActiveAsync(
        int universityId,
        CancellationToken ct = default)
    {
        var university = await _uow.Universities.GetByIdAsync(universityId, ct);
        if (university is null)
            return ServiceResult.Failure("University not found.");

        university.IsActive = !university.IsActive;
        _uow.Universities.Update(university);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "University {Id} toggled to IsActive={IsActive}",
            universityId, university.IsActive);

        return ServiceResult.Success();
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    public async Task<ServiceResult> DeleteAsync(
        int universityId,
        CancellationToken ct = default)
    {
        var university = await _uow.Universities.GetByIdAsync(universityId, ct);
        if (university is null)
            return ServiceResult.Failure("University not found.");

        // Guard: cannot delete if students or properties are associated
        var hasStudents = await _uow.Students.ExistsAsync(
            s => s.UniversityId == universityId, ct);
        var hasProperties = await _uow.Properties.ExistsAsync(
            p => p.UniversityId == universityId, ct);

        if (hasStudents || hasProperties)
            return ServiceResult.Failure(
                "Cannot delete this university because students or properties " +
                "are currently associated with it. Deactivate it instead.");

        _uow.Universities.Remove(university);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("University deleted: Id={Id}", universityId);
        return ServiceResult.Success();
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private async Task<IReadOnlyList<UniversityDto>> EnrichWithStatsAsync(
        IReadOnlyList<University> universities,
        CancellationToken ct)
    {
        var dtos = new List<UniversityDto>();

        foreach (var u in universities)
        {
            var dto = _mapper.Map<UniversityDto>(u);
            dto = dto with
            {
                TotalProperties = await _uow.Properties.CountAsync(
                    p => p.UniversityId == u.Id && p.IsApproved, ct),
                TotalStudents = await _uow.Students.CountAsync(
                    s => s.UniversityId == u.Id, ct)
            };
            dtos.Add(dto);
        }

        return dtos;
    }
}