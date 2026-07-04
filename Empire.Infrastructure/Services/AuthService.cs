using Microsoft.EntityFrameworkCore;
using Empire.Application.DTOs.Auth;
using Empire.Application.Interfaces;
using Empire.Domain.Interfaces;
using Empire.Infrastructure.Data;

namespace Empire.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHashService _passwordHashService;
    private readonly EmpireDbContext _context;

    public AuthService(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IPasswordHashService passwordHashService,
        EmpireDbContext context)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _passwordHashService = passwordHashService;
        _context = context;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);
        if (user == null || !user.IsActive)
            return null;

        if (!_passwordHashService.VerifyPassword(request.Password, user.PasswordHash))
            return null;

        // Get user's shop roles
        var shopRoles = await _context.UserShopRoles
            .Include(usr => usr.Shop)
            .Include(usr => usr.Role)
            .Where(usr => usr.UserId == user.Id && usr.IsActive && !usr.IsDeleted)
            .ToListAsync();

        // Map shop roles to DTOs
        var shopRoleDtos = shopRoles.Select(sr => new UserShopRoleDto
        {
            ShopId = sr.ShopId,
            ShopName = sr.Shop.Name,
            RoleId = sr.RoleId,
            RoleName = sr.Role?.Name ?? "Viewer",
            IsActive = sr.IsActive
        }).ToList();

        var accessToken = _jwtTokenService.GenerateToken(new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive
        }, shopRoleDtos);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = string.Empty, // TODO: Implement refresh tokens
            ExpiresAt = DateTime.UtcNow.AddMinutes(480),
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive
            },
            ShopRoles = shopRoleDtos
        };
    }

    public async Task<LoginResponse?> RefreshTokenAsync(string refreshToken)
    {
        // In a real implementation, you would store refresh tokens in the database
        // and validate them here. For now, we'll return null to indicate invalid token.
        await Task.CompletedTask;
        return null;
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        // In a real implementation, you would invalidate the refresh token here
        await Task.CompletedTask;
        return true;
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        var principal = _jwtTokenService.ValidateToken(token);
        return await Task.FromResult(principal != null);
    }

    public async Task<UserDto?> GetCurrentUserAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || !user.IsActive)
            return null;

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive
        };
    }
}

