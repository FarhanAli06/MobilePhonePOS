using Empire.Application.DTOs.Auth;
using Empire.Application.DTOs.User;
using Empire.Application.Interfaces;
using Empire.Domain.Entities;
using Empire.Domain.Enums;
using Empire.Domain.Interfaces;

namespace Empire.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHashService _passwordHashService;

    public UserService(IUserRepository userRepository, IPasswordHashService passwordHashService)
    {
        _userRepository = userRepository;
        _passwordHashService = passwordHashService;
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user != null ? MapToUserDto(user) : null;
    }

    public async Task<UserDto?> GetUserByUsernameAsync(string username)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        return user != null ? MapToUserDto(user) : null;
    }

    public async Task<IEnumerable<UserDto>> GetUsersByShopAsync(int shopId)
    {
        var users = await _userRepository.GetUsersByShopAsync(shopId);
        return users.Select(MapToUserDto);
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(MapToUserDto);
    }

    public async Task<IEnumerable<UserDto>> GetFilteredUsersAsync(UserFilterRequest filter)
    {
        var users = await _userRepository.GetAllAsync();
        
        var query = users.AsQueryable();

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            query = query.Where(u => u.Username.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                                   u.FirstName.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                                   u.LastName.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                                   u.Email.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.ShopId.HasValue)
        {
            query = query.Where(u => u.UserShopRoles.Any(usr => usr.ShopId == filter.ShopId.Value));
        }

        if (filter.Role.HasValue)
        {
            query = query.Where(u => u.UserShopRoles.Any(usr => usr.Role == filter.Role.Value));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == filter.IsActive.Value);
        }

        if (filter.CreatedFrom.HasValue)
        {
            query = query.Where(u => u.CreatedDate >= filter.CreatedFrom.Value);
        }

        if (filter.CreatedTo.HasValue)
        {
            query = query.Where(u => u.CreatedDate <= filter.CreatedTo.Value);
        }

        return query.Select(MapToUserDto);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, int createdByUserId)
    {
        // Check if username already exists
        if (await _userRepository.UsernameExistsAsync(request.Username))
        {
            throw new ArgumentException($"Username '{request.Username}' already exists");
        }

        // Check if email already exists
        if (await _userRepository.EmailExistsAsync(request.Email))
        {
            throw new ArgumentException($"Email '{request.Email}' already exists");
        }

        var hashedPassword = _passwordHashService.HashPassword(request.Password);

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = hashedPassword,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            IsActive = request.IsActive,
            CreatedDate = DateTime.UtcNow
        };

        var createdUser = await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        // Assign user to shop with role
        await AssignUserToShopAsync(createdUser.Id, request.ShopId, request.Role);

        return MapToUserDto(createdUser);
    }

    public async Task<UserDto> UpdateUserAsync(int id, UpdateUserRequest request, int modifiedByUserId)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            throw new ArgumentException($"User with ID {id} not found");
        }

        // Check if email already exists for another user
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null && existingUser.Id != id)
        {
            throw new ArgumentException($"Email '{request.Email}' already exists");
        }

        user.Email = request.Email;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Phone = request.Phone;
        user.IsActive = request.IsActive;
        user.ModifiedDate = DateTime.UtcNow;

        // Update password if provided
        if (!string.IsNullOrEmpty(request.NewPassword))
        {
            user.PasswordHash = _passwordHashService.HashPassword(request.NewPassword);
        }

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        // Update user role in shop
        await UpdateUserRoleInShopAsync(id, request.ShopId, request.Role);

        return MapToUserDto(user);
    }

    public async Task DeleteUserAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            throw new ArgumentException($"User with ID {id} not found");
        }

        // Soft delete
        user.IsDeleted = true;
        user.ModifiedDate = DateTime.UtcNow;
        
        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await _userRepository.UsernameExistsAsync(username);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _userRepository.EmailExistsAsync(email);
    }

    public async Task AssignUserToShopAsync(int userId, int shopId, UserRole role)
    {
        var userShopRole = new UserShopRole
        {
            UserId = userId,
            ShopId = shopId,
            Role = role,
            CreatedDate = DateTime.UtcNow
        };

        await _userRepository.AddUserShopRoleAsync(userShopRole);
    }

    public async Task RemoveUserFromShopAsync(int userId, int shopId)
    {
        await _userRepository.RemoveUserShopRoleAsync(userId, shopId);
    }

    public async Task UpdateUserRoleInShopAsync(int userId, int shopId, UserRole role)
    {
        await _userRepository.UpdateUserShopRoleAsync(userId, shopId, role);
    }

    private static UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Phone = user.Phone,
            IsActive = user.IsActive,
            CreatedDate = user.CreatedDate,
            ModifiedDate = user.ModifiedDate,
            ShopRoles = user.UserShopRoles?.Select(usr => new UserShopRoleDto
            {
                ShopId = usr.ShopId,
                ShopName = usr.Shop?.Name ?? "",
                Role = usr.Role
            }).ToList() ?? new List<UserShopRoleDto>()
        };
    }
}

