//using System.Collections.ObjectModel;
//using System.Linq;
//using System.Text.Json;
//using System.Threading;
//using Microsoft.Maui.Controls;
//using CommunityToolkit.Mvvm.ComponentModel;
//using CommunityToolkit.Mvvm.Input;
//using NavQurt.Server.App.Services;

//namespace NavQurt.Server.App.ViewModels;

//public partial class DashboardViewModel : ObservableObject
//{
//    private readonly GeneralApi _api;
//    private readonly JsonSerializerOptions _jsonOptions = new()
//    {
//        WriteIndented = true
//    };

//    public DashboardViewModel(GeneralApi api)
//    {
//        _api = api;
//    }

//    public ObservableCollection<UserDto> Users { get; } = new();

//    public ObservableCollection<RoleDto> Roles { get; } = new();

//    [ObservableProperty]
//    private bool isBusy;

//    [ObservableProperty]
//    private string? sessionSummary;

//    [ObservableProperty]
//    private string? infoMessage;

//    [ObservableProperty]
//    private string? errorMessage;

//    [ObservableProperty]
//    private UserDto? selectedUser;

//    [ObservableProperty]
//    private RoleDto? selectedRole;

//    [ObservableProperty]
//    private string userIdInput = string.Empty;

//    [ObservableProperty]
//    private string userNameInput = string.Empty;

//    [ObservableProperty]
//    private string? firstNameInput;

//    [ObservableProperty]
//    private string? lastNameInput;

//    [ObservableProperty]
//    private string? emailInput;

//    [ObservableProperty]
//    private string? phoneInput;

//    [ObservableProperty]
//    private bool userIsActiveInput = true;

//    [ObservableProperty]
//    private bool applyUserIsActive;

//    [ObservableProperty]
//    private string? userDetailsJson;

//    [ObservableProperty]
//    private string roleIdInput = string.Empty;

//    [ObservableProperty]
//    private string roleNameInput = string.Empty;

//    [ObservableProperty]
//    private string? roleDetailsJson;

//    [ObservableProperty]
//    private string roleAssignmentUserId = string.Empty;

//    [ObservableProperty]
//    private string roleAssignmentRoles = string.Empty;

//    [ObservableProperty]
//    private string? roleAssignmentResult;

//    [ObservableProperty]
//    private string changePasswordUserId = string.Empty;

//    [ObservableProperty]
//    private string changePasswordCurrent = string.Empty;

//    [ObservableProperty]
//    private string changePasswordNew = string.Empty;

//    [ObservableProperty]
//    private string forgotPasswordEmail = string.Empty;

//    [ObservableProperty]
//    private string? generatedPasswordToken;

//    [ObservableProperty]
//    private string resetPasswordUserId = string.Empty;

//    [ObservableProperty]
//    private string resetPasswordToken = string.Empty;

//    [ObservableProperty]
//    private string resetPasswordNew = string.Empty;

//    [ObservableProperty]
//    private string emailTokenUserId = string.Empty;

//    [ObservableProperty]
//    private string? generatedEmailToken;

//    [ObservableProperty]
//    private string emailVerificationToken = string.Empty;

//    [ObservableProperty]
//    private string phoneTokenUserId = string.Empty;

//    [ObservableProperty]
//    private string phoneTokenPhoneNumber = string.Empty;

//    [ObservableProperty]
//    private string? generatedPhoneToken;

//    [ObservableProperty]
//    private string phoneVerificationToken = string.Empty;

//    [RelayCommand]
//    private async Task RefreshSessionAsync()
//    {
//        await ExecuteAsync(async () =>
//        {
//            var tokens = await _api.GetSavedTokensAsync();
//            if (tokens is null)
//            {
//                SessionSummary = "Not signed in.";
//                return;
//            }

//            SessionSummary =
//                $"Signed in as {tokens.UserName} (access expires {tokens.AccessTokenExpiresAt:G}, refresh expires {tokens.RefreshTokenExpiresAt:G}).";

//            if (string.IsNullOrWhiteSpace(ChangePasswordUserId))
//            {
//                ChangePasswordUserId = tokens.UserId;
//            }

//            if (string.IsNullOrWhiteSpace(RoleAssignmentUserId))
//            {
//                RoleAssignmentUserId = tokens.UserId;
//            }

//            if (string.IsNullOrWhiteSpace(EmailTokenUserId))
//            {
//                EmailTokenUserId = tokens.UserId;
//            }

//            if (string.IsNullOrWhiteSpace(PhoneTokenUserId))
//            {
//                PhoneTokenUserId = tokens.UserId;
//            }

//            if (string.IsNullOrWhiteSpace(ResetPasswordUserId))
//            {
//                ResetPasswordUserId = tokens.UserId;
//            }
//        });
//    }

//    [RelayCommand]
//    private Task LoadUsersAsync()
//        => ExecuteAsync(async () =>
//        {
//            var users = await _api.GetUsersAsync(CancellationToken.None);
//            Users.Clear();
//            foreach (var user in users.OrderBy(u => u.UserName, StringComparer.OrdinalIgnoreCase))
//            {
//                Users.Add(user);
//            }

//            InfoMessage = $"Loaded {Users.Count} user(s).";
//        });

//    [RelayCommand]
//    private Task LoadRolesAsync()
//        => ExecuteAsync(async () =>
//        {
//            var roles = await _api.GetRolesAsync(CancellationToken.None);
//            Roles.Clear();
//            foreach (var role in roles.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
//            {
//                Roles.Add(role);
//            }

//            InfoMessage = $"Loaded {Roles.Count} role(s).";
//        });

//    [RelayCommand]
//    private Task FetchUserAsync()
//        => ExecuteAsync(async () =>
//        {
//            if (string.IsNullOrWhiteSpace(UserIdInput))
//            {
//                throw new InvalidOperationException("Enter a user ID.");
//            }

//            var user = await _api.GetUserAsync(UserIdInput.Trim(), CancellationToken.None);
//            ApplyUser(user);
//            var listUser = Users.FirstOrDefault(u => u.Id == user.Id);
//            if (listUser is not null)
//            {
//                SelectedUser = listUser;
//            }
//            InfoMessage = $"Loaded details for {user.UserName}.";
//        });

//    [RelayCommand]
//    private Task UpdateUserAsync()
//        => ExecuteAsync(async () =>
//        {
//            if (string.IsNullOrWhiteSpace(UserIdInput))
//            {
//                throw new InvalidOperationException("Enter a user ID.");
//            }

//            var user = await _api.UpdateUserProfileAsync(UserIdInput.Trim(),
//                string.IsNullOrWhiteSpace(UserNameInput) ? null : UserNameInput.Trim(),
//                string.IsNullOrWhiteSpace(FirstNameInput) ? null : FirstNameInput.Trim(),
//                string.IsNullOrWhiteSpace(LastNameInput) ? null : LastNameInput.Trim(),
//                string.IsNullOrWhiteSpace(EmailInput) ? null : EmailInput.Trim(),
//                string.IsNullOrWhiteSpace(PhoneInput) ? null : PhoneInput.Trim(),
//                ApplyUserIsActive ? UserIsActiveInput : (bool?)null,
//                CancellationToken.None);

//            ApplyUser(user);
//            UpdateUsersCollection(user);
//            InfoMessage = "User profile updated.";
//        });

//    [RelayCommand]
//    private Task SetUserStatusAsync()
//        => ExecuteAsync(async () =>
//        {
//            if (string.IsNullOrWhiteSpace(UserIdInput))
//            {
//                throw new InvalidOperationException("Enter a user ID.");
//            }

//            var user = await _api.SetUserStatusAsync(UserIdInput.Trim(), UserIsActiveInput, CancellationToken.None);
//            ApplyUser(user);
//            UpdateUsersCollection(user);
//            InfoMessage = $"User {(user.IsActive ? "activated" : "deactivated")}.";
//        });

//    [RelayCommand]
//    private Task DeleteUserAsync()
//        => ExecuteAsync(async () =>
//        {
//            if (string.IsNullOrWhiteSpace(UserIdInput))
//            {
//                throw new InvalidOperationException("Enter a user ID.");
//            }

//            await _api.DeleteUserAsync(UserIdInput.Trim(), CancellationToken.None);
//            InfoMessage = "User deleted.";
//            var existing = Users.FirstOrDefault(u => u.Id == UserIdInput.Trim());
//            if (existing is not null)
//            {
//                Users.Remove(existing);
//            }
//        });

//    [RelayCommand]
//    private Task UpdateUserRolesAsync()
//        => ExecuteAsync(async () =>
//        {
//            if (string.IsNullOrWhiteSpace(RoleAssignmentUserId))
//            {
//                throw new InvalidOperationException("Enter a user ID.");
//            }

//            var roles = ParseRoles(RoleAssignmentRoles);
//            if (roles.Count == 0)
//            {
//                throw new InvalidOperationException("Enter one or more role names (comma separated).");
//            }

//            var result = await _api.UpdateUserRolesAsync(RoleAssignmentUserId.Trim(), roles, CancellationToken.None);
//            RoleAssignmentResult = FormatJson(result);
//            InfoMessage = "User roles replaced.";
//        });

//    [RelayCommand]
//    private Task AssignRolesAsync()
//        => ExecuteAsync(async () =>
//        {
//            if (string.IsNullOrWhiteSpace(RoleAssignmentUserId))
//            {
//                throw new InvalidOperationException("Enter a user ID.");
//            }

//            var roles = ParseRoles(RoleAssignmentRoles);
//            if (roles.Count == 0)
//            {
//                throw new InvalidOperationException("Enter one or more role names (comma separated).");
//            }

//            var result = await _api.AssignRolesAsync(RoleAssignmentUserId.Trim(), roles, CancellationToken.None);
//            RoleAssignmentResult = FormatJson(result);
//            InfoMessage = "Roles assigned.";
//        });

//    [RelayCommand]
//    private Task RemoveRolesAsync()
//        => ExecuteAsync(async () =>
//        {
//            if (string.IsNullOrWhiteSpace(RoleAssignmentUserId))
//            {
//                throw new InvalidOperationException("Enter a user ID.");
//            }

//            var roles = ParseRoles(RoleAssignmentRoles);
//            if (roles.Count == 0)
//            {
//                throw new InvalidOperationException("Enter one or more role names (comma separated).");
//            }

//            var result = await _api.RemoveRolesAsync(RoleAssignmentUserId.Trim(), roles, CancellationToken.None);
//            RoleAssignmentResult = FormatJson(result);
//            InfoMessage = "Roles removed.";
//        });

//    [RelayCommand]
//    private Task GetUserRolesAsync()
//        => ExecuteAsync(async () =>
//        {
//            if (string.IsNullOrWhiteSpace(RoleAssignmentUserId))
//            {
//                throw new InvalidOperationException("Enter a user ID.");
//            }

//            var result = await _api.GetUserRolesAsync(RoleAssignmentUserId.Trim(), CancellationToken.None);
//            RoleAssignmentResult = FormatJson(result);
//            InfoMessage = "Fetched user roles.";
//        });

//    [RelayCommand]
//    private Task CreateRoleAsync()
//        => ExecuteAsync(async () =>
//        {
//            if (string.IsNullOrWhiteSpace(RoleNameInput))
//            {
//                throw new InvalidOperationException("Enter a role name.");
//            }

//            var role = await _api.CreateRoleAsync(RoleNameInput.Trim(), CancellationToken.None);
//            UpdateRolesCollection(role);
//            ApplyRole(role);
//            InfoMessage = $"Role {role.Name} created.";
//        });

//    [RelayCommand]
//    private Task UpdateRoleAsync()
//        => ExecuteAsync(async () =>
//        {
//            if (string.IsNullOrWhiteSpace(RoleIdInput))
//            {
//                throw new InvalidOperationException("Enter a role ID.");
//            }

//            if (string.IsNullOrWhiteSpace(RoleNameInput))
//            {
//                throw new InvalidOperationException("Enter a role name.");
//            }

//            var role = await _api.UpdateRoleAsync(RoleIdInput.Trim(), RoleNameInput.Trim(), CancellationToken.None);
//            ApplyRole(role);
//            UpdateRolesCollection(role);
//            InfoMessage = "Role updated.";
//        });

//    [RelayCommand]
//    private Task DeleteRoleAsync()
//        => ExecuteAsync(async () =>
//        {
//            if (string.IsNullOrWhiteSpace(RoleIdInput))
//            {
//                throw new InvalidOperationException("Enter a role ID.");
//            }

//            await _api.DeleteRoleAsync(RoleIdInput.Trim(), CancellationToken.None);
//            InfoMessage = "Role deleted.";
//            var item = Roles.FirstOrDefault(r => r.Id == RoleIdInput.Trim());
//            if (item is not null)
//            {
//                Roles.Remove(item);
//            }
//        });

//    [RelayCommand]
//    private Task ChangePasswordAsync()
//        => ExecuteAsync(async () =>
//        {
//            if (string.IsNullOrWhiteSpace(ChangePasswordUserId))
//            {
//                throw new InvalidOperationException("Enter a user ID.");
//            }

//            if (string.IsNullOrWhiteSpace(ChangePasswordCurrent) || string.IsNullOrWhiteSpace(ChangePasswordNew))
//            {
//                throw new InvalidOperationException("Provide both the current and new passwords.");
//            }

//            await _api.ChangePasswordAsync(ChangePasswordUserId.Trim(), ChangePasswordCurrent, ChangePasswordNew,
//                CancellationToken.None);

//            InfoMessage = "Password changed.";
//            ChangePasswordCurrent = string.Empty;
//            ChangePasswordNew = string.Empty;
//        });

//    [RelayCommand]
//    private Task ForgotPasswordAsync()
//        => ExecuteAsync(async () =>
//        {
//            if (string.IsNullOrWhiteSpace(ForgotPasswordEmail))
//            {
//                throw new InvalidOperationException("Enter an email address.");
//            }

//            var token = await _api.ForgotPasswordAsync(ForgotPasswordEmail.Trim(), CancellationToken.None);
//            GeneratedPasswordToken = token.Token;
//            ResetPasswordUserId = token.UserId;
//            InfoMessage = "Password reset token generated.";
//        });

//    [RelayCommand]
//    private Task ResetPasswordAsync()
//        => ExecuteAsync(async () =>
//        {
//            if (string.IsNullOrWhiteSpace(ResetPasswordUserId) || string.IsNullOrWhiteSpace(ResetPasswordToken) ||
//                string.IsNullOrWhiteSpace(ResetPasswordNew))
//            {
//                throw new InvalidOperationException("Provide user ID, token, and new password.");
//            }

//            var success = await _api.ResetPasswordAsync(ResetPasswordUserId.Trim(), ResetPasswordToken, ResetPasswordNew,
//                CancellationToken.None);

//            InfoMessage = success ? "Password reset." : "Password reset request sent.";
//        });

//    [RelayCommand]
//    private Task GenerateEmailTokenAsync()
//        => ExecuteAsync(async () =>
//        {
//            if (string.IsNullOrWhiteSpace(EmailTokenUserId))
//            {
//                throw new InvalidOperationException("Enter a user ID.");
//            }

//            var token = await _api.GenerateEmailTokenAsync(EmailTokenUserId.Trim(), CancellationToken.None);
//            GeneratedEmailToken = token.Token;
//            InfoMessage = "Email verification token generated.";
//        });

//    [RelayCommand]
//    private Task VerifyEmailAsync()
//        => ExecuteAsync(async () =>
//        {
//            if (string.IsNullOrWhiteSpace(EmailTokenUserId) || string.IsNullOrWhiteSpace(EmailVerificationToken))
//            {
//                throw new InvalidOperationException("Provide user ID and token.");
//            }

//            var success = await _api.VerifyEmailAsync(EmailTokenUserId.Trim(), EmailVerificationToken,
//                CancellationToken.None);

//            InfoMessage = success ? "Email verified." : "Verification request sent.";
//        });

//    [RelayCommand]
//    private Task GeneratePhoneTokenAsync()
//        => ExecuteAsync(async () =>
//        {
//            if (string.IsNullOrWhiteSpace(PhoneTokenUserId))
//            {
//                throw new InvalidOperationException("Enter a user ID.");
//            }

//            var token = await _api.GeneratePhoneTokenAsync(PhoneTokenUserId.Trim(),
//                string.IsNullOrWhiteSpace(PhoneTokenPhoneNumber) ? null : PhoneTokenPhoneNumber.Trim(),
//                CancellationToken.None);

//            GeneratedPhoneToken = token.Token;
//            PhoneTokenPhoneNumber = token.PhoneNumber;
//            InfoMessage = "Phone verification token generated.";
//        });

//    [RelayCommand]
//    private Task VerifyPhoneAsync()
//        => ExecuteAsync(async () =>
//        {
//            if (string.IsNullOrWhiteSpace(PhoneTokenUserId) || string.IsNullOrWhiteSpace(PhoneTokenPhoneNumber) ||
//                string.IsNullOrWhiteSpace(PhoneVerificationToken))
//            {
//                throw new InvalidOperationException("Provide user ID, phone number, and token.");
//            }

//            var success = await _api.VerifyPhoneAsync(PhoneTokenUserId.Trim(), PhoneTokenPhoneNumber.Trim(),
//                PhoneVerificationToken.Trim(), CancellationToken.None);

//            InfoMessage = success ? "Phone verified." : "Verification request sent.";
//        });

//    [RelayCommand]
//    private async Task LogoutAsync()
//    {
//        await ExecuteAsync(async () =>
//        {
//            await _api.LogoutAsync(CancellationToken.None);
//            SessionSummary = "Not signed in.";
//            Users.Clear();
//            Roles.Clear();
//            InfoMessage = "Signed out.";
//            await Shell.Current.GoToAsync("//signin");
//        });
//    }

//    partial void OnSelectedUserChanged(UserDto? value)
//    {
//        if (value is not null)
//        {
//            ApplyUser(value);
//        }
//    }

//    partial void OnSelectedRoleChanged(RoleDto? value)
//    {
//        if (value is not null)
//        {
//            ApplyRole(value);
//        }
//    }

//    private void ApplyUser(UserDto user)
//    {
//        UserIdInput = user.Id;
//        UserNameInput = user.UserName;
//        FirstNameInput = user.FirstName;
//        LastNameInput = user.LastName;
//        EmailInput = user.Email;
//        PhoneInput = user.PhoneNumber;
//        UserIsActiveInput = user.IsActive;
//        ApplyUserIsActive = true;
//        UserDetailsJson = FormatJson(user);
//    }

//    private void ApplyRole(RoleDto role)
//    {
//        RoleIdInput = role.Id;
//        RoleNameInput = role.Name;
//        RoleDetailsJson = FormatJson(role);
//    }

//    private void UpdateUsersCollection(UserDto user)
//    {
//        var existing = Users.FirstOrDefault(u => u.Id == user.Id);
//        if (existing is not null)
//        {
//            var index = Users.IndexOf(existing);
//            if (index >= 0)
//            {
//                Users[index] = user;
//            }
//        }
//        else
//        {
//            Users.Add(user);
//        }
//    }

//    private void UpdateRolesCollection(RoleDto role)
//    {
//        var existing = Roles.FirstOrDefault(r => r.Id == role.Id);
//        if (existing is not null)
//        {
//            var index = Roles.IndexOf(existing);
//            if (index >= 0)
//            {
//                Roles[index] = role;
//            }
//        }
//        else
//        {
//            Roles.Add(role);
//        }
//    }

//    private string FormatJson<T>(T value)
//        => JsonSerializer.Serialize(value, _jsonOptions);

//    private static IList<string> ParseRoles(string roles)
//    {
//        return roles.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
//            .Select(r => r)
//            .ToList();
//    }

//    private async Task ExecuteAsync(Func<Task> action)
//    {
//        try
//        {
//            IsBusy = true;
//            ErrorMessage = null;
//            InfoMessage = null;
//            await action();
//        }
//        catch (Exception ex)
//        {
//            ErrorMessage = ex.Message;
//        }
//        finally
//        {
//            IsBusy = false;
//        }
//    }
//}
