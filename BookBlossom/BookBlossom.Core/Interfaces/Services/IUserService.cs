using System.Collections.Generic;
using System.Threading.Tasks;
using BookBlossom.Core.DTOs;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface IUserService
    {
        Task<bool> UpdateProfileAsync(long userId, UpdateProfileDTO dto);
        Task<bool> ChangePasswordAsync(long userId, ChangePasswordRequestDTO dto);
        Task<IEnumerable<AddressDTO>> GetAddressesAsync(long userId);
        Task<AddressDTO> AddAddressAsync(long userId, AddressDTO dto);
        Task<bool> UpdateAddressAsync(long userId, long addressId, AddressDTO dto);
        Task<bool> DeleteAddressAsync(long userId, long addressId);
        Task<bool> SetDefaultAddressAsync(long userId, long addressId);
    }
}
