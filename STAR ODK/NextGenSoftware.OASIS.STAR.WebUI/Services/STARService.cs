using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.STAR.CLI.Lib;
using Microsoft.AspNetCore.SignalR;
using NextGenSoftware.OASIS.STAR.WebUI.Hubs;

namespace NextGenSoftware.OASIS.STAR.WebUI.Services
{
    public class STARService : ISTARService
    {
        private readonly IHubContext<STARHub> _hubContext;

        public STARService(IHubContext<STARHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task<OASISResult<IOmiverse>> IgniteSTARAsync()
        {
            try
            {
                var result = await STARCLI.IgniteSTARAsync();
                
                if (!result.IsError)
                {
                    await _hubContext.Clients.All.SendAsync("STARIgnited", result.Result);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                return new OASISResult<IOmiverse>
                {
                    IsError = true,
                    Message = $"Error igniting STAR: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<OASISResult<bool>> ExtinguishStarAsync()
        {
            try
            {
                var result = await STARCLI.ExtinguishStarAsync();
                
                if (!result.IsError)
                {
                    await _hubContext.Clients.All.SendAsync("STARExtinguished", result.Result);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error extinguishing STAR: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<OASISResult<bool>> IsSTARIgnitedAsync()
        {
            try
            {
                return new OASISResult<bool>
                {
                    Result = STARCLI.IsSTARIgnited,
                    IsError = false
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error checking STAR status: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<OASISResult<IAvatar>> GetBeamedInAvatarAsync()
        {
            try
            {
                if (!STARCLI.IsSTARIgnited)
                {
                    return new OASISResult<IAvatar>
                    {
                        IsError = true,
                        Message = "STAR is not ignited. Please ignite STAR first."
                    };
                }

                var avatar = STAR.BeamedInAvatar;
                return new OASISResult<IAvatar>
                {
                    Result = avatar,
                    IsError = false
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsError = true,
                    Message = $"Error getting beamed in avatar: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<OASISResult<IAvatar>> BeamInAvatarAsync()
        {
            try
            {
                if (!STARCLI.IsSTARIgnited)
                {
                    return new OASISResult<IAvatar>
                    {
                        IsError = true,
                        Message = "STAR is not ignited. Please ignite STAR first."
                    };
                }

                await STARCLI.Avatars.BeamInAvatar();
                var avatar = STAR.BeamedInAvatar;
                
                if (avatar != null)
                {
                    await _hubContext.Clients.All.SendAsync("AvatarBeamedIn", avatar);
                }
                
                return new OASISResult<IAvatar>
                {
                    Result = avatar,
                    IsError = false
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsError = true,
                    Message = $"Error beaming in avatar: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<OASISResult<IAvatar>> CreateAvatarAsync(string username, string email, string password)
        {
            try
            {
                if (!STARCLI.IsSTARIgnited)
                {
                    return new OASISResult<IAvatar>
                    {
                        IsError = true,
                        Message = "STAR is not ignited. Please ignite STAR first."
                    };
                }

                var result = await STARCLI.Avatars.CreateAvatarAsync(username, email, password);
                
                if (!result.IsError && result.Result != null)
                {
                    await _hubContext.Clients.All.SendAsync("AvatarCreated", result.Result);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsError = true,
                    Message = $"Error creating avatar: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id)
        {
            try
            {
                if (!STARCLI.IsSTARIgnited)
                {
                    return new OASISResult<IAvatar>
                    {
                        IsError = true,
                        Message = "STAR is not ignited. Please ignite STAR first."
                    };
                }

                return await STARCLI.Avatars.LoadAvatarAsync(id);
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsError = true,
                    Message = $"Error loading avatar: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username)
        {
            try
            {
                if (!STARCLI.IsSTARIgnited)
                {
                    return new OASISResult<IAvatar>
                    {
                        IsError = true,
                        Message = "STAR is not ignited. Please ignite STAR first."
                    };
                }

                return await STARCLI.Avatars.LoadAvatarAsync(username);
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsError = true,
                    Message = $"Error loading avatar: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password)
        {
            try
            {
                if (!STARCLI.IsSTARIgnited)
                {
                    return new OASISResult<IAvatar>
                    {
                        IsError = true,
                        Message = "STAR is not ignited. Please ignite STAR first."
                    };
                }

                return await STARCLI.Avatars.LoadAvatarAsync(username, password);
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsError = true,
                    Message = $"Error loading avatar: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            try
            {
                if (!STARCLI.IsSTARIgnited)
                {
                    return new OASISResult<IAvatar>
                    {
                        IsError = true,
                        Message = "STAR is not ignited. Please ignite STAR first."
                    };
                }

                var result = await STARCLI.Avatars.SaveAvatarAsync(avatar);
                
                if (!result.IsError && result.Result != null)
                {
                    await _hubContext.Clients.All.SendAsync("AvatarSaved", result.Result);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsError = true,
                    Message = $"Error saving avatar: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id)
        {
            try
            {
                if (!STARCLI.IsSTARIgnited)
                {
                    return new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "STAR is not ignited. Please ignite STAR first."
                    };
                }

                var result = await STARCLI.Avatars.DeleteAvatarAsync(id);
                
                if (!result.IsError && result.Result)
                {
                    await _hubContext.Clients.All.SendAsync("AvatarDeleted", id);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Error deleting avatar: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<OASISResult<List<IAvatar>>> LoadAllAvatarsAsync()
        {
            try
            {
                if (!STARCLI.IsSTARIgnited)
                {
                    return new OASISResult<List<IAvatar>>
                    {
                        IsError = true,
                        Message = "STAR is not ignited. Please ignite STAR first."
                    };
                }

                return await STARCLI.Avatars.LoadAllAvatarsAsync();
            }
            catch (Exception ex)
            {
                return new OASISResult<List<IAvatar>>
                {
                    IsError = true,
                    Message = $"Error loading all avatars: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<OASISResult<List<IAvatar>>> SearchAvatarsAsync(string searchTerm)
        {
            try
            {
                if (!STARCLI.IsSTARIgnited)
                {
                    return new OASISResult<List<IAvatar>>
                    {
                        IsError = true,
                        Message = "STAR is not ignited. Please ignite STAR first."
                    };
                }

                return await STARCLI.Avatars.SearchAvatarsAsync(searchTerm);
            }
            catch (Exception ex)
            {
                return new OASISResult<List<IAvatar>>
                {
                    IsError = true,
                    Message = $"Error searching avatars: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<OASISResult<IKarmaAkashicRecord>> GetKarmaAsync(Guid avatarId)
        {
            try
            {
                if (!STARCLI.IsSTARIgnited)
                {
                    return new OASISResult<IKarmaAkashicRecord>
                    {
                        IsError = true,
                        Message = "STAR is not ignited. Please ignite STAR first."
                    };
                }

                return await STARCLI.Avatars.GetKarmaAsync(avatarId);
            }
            catch (Exception ex)
            {
                return new OASISResult<IKarmaAkashicRecord>
                {
                    IsError = true,
                    Message = $"Error getting karma: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<OASISResult<IKarmaAkashicRecord>> AddKarmaAsync(Guid avatarId, int karma)
        {
            try
            {
                if (!STARCLI.IsSTARIgnited)
                {
                    return new OASISResult<IKarmaAkashicRecord>
                    {
                        IsError = true,
                        Message = "STAR is not ignited. Please ignite STAR first."
                    };
                }

                var result = await STARCLI.Avatars.AddKarmaAsync(avatarId, karma);
                
                if (!result.IsError && result.Result != null)
                {
                    await _hubContext.Clients.All.SendAsync("KarmaAdded", new { AvatarId = avatarId, Karma = karma, Result = result.Result });
                }
                
                return result;
            }
            catch (Exception ex)
            {
                return new OASISResult<IKarmaAkashicRecord>
                {
                    IsError = true,
                    Message = $"Error adding karma: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<OASISResult<IKarmaAkashicRecord>> RemoveKarmaAsync(Guid avatarId, int karma)
        {
            try
            {
                if (!STARCLI.IsSTARIgnited)
                {
                    return new OASISResult<IKarmaAkashicRecord>
                    {
                        IsError = true,
                        Message = "STAR is not ignited. Please ignite STAR first."
                    };
                }

                var result = await STARCLI.Avatars.RemoveKarmaAsync(avatarId, karma);
                
                if (!result.IsError && result.Result != null)
                {
                    await _hubContext.Clients.All.SendAsync("KarmaRemoved", new { AvatarId = avatarId, Karma = karma, Result = result.Result });
                }
                
                return result;
            }
            catch (Exception ex)
            {
                return new OASISResult<IKarmaAkashicRecord>
                {
                    IsError = true,
                    Message = $"Error removing karma: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<OASISResult<IKarmaAkashicRecord>> SetKarmaAsync(Guid avatarId, int karma)
        {
            try
            {
                if (!STARCLI.IsSTARIgnited)
                {
                    return new OASISResult<IKarmaAkashicRecord>
                    {
                        IsError = true,
                        Message = "STAR is not ignited. Please ignite STAR first."
                    };
                }

                var result = await STARCLI.Avatars.SetKarmaAsync(avatarId, karma);
                
                if (!result.IsError && result.Result != null)
                {
                    await _hubContext.Clients.All.SendAsync("KarmaSet", new { AvatarId = avatarId, Karma = karma, Result = result.Result });
                }
                
                return result;
            }
            catch (Exception ex)
            {
                return new OASISResult<IKarmaAkashicRecord>
                {
                    IsError = true,
                    Message = $"Error setting karma: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<OASISResult<List<IKarmaAkashicRecord>>> GetAllKarmaAsync()
        {
            try
            {
                if (!STARCLI.IsSTARIgnited)
                {
                    return new OASISResult<List<IKarmaAkashicRecord>>
                    {
                        IsError = true,
                        Message = "STAR is not ignited. Please ignite STAR first."
                    };
                }

                return await STARCLI.Avatars.GetAllKarmaAsync();
            }
            catch (Exception ex)
            {
                return new OASISResult<List<IKarmaAkashicRecord>>
                {
                    IsError = true,
                    Message = $"Error getting all karma: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<OASISResult<List<IKarmaAkashicRecord>>> GetKarmaBetweenAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                if (!STARCLI.IsSTARIgnited)
                {
                    return new OASISResult<List<IKarmaAkashicRecord>>
                    {
                        IsError = true,
                        Message = "STAR is not ignited. Please ignite STAR first."
                    };
                }

                return await STARCLI.Avatars.GetKarmaBetweenAsync(fromDate, toDate);
            }
            catch (Exception ex)
            {
                return new OASISResult<List<IKarmaAkashicRecord>>
                {
                    IsError = true,
                    Message = $"Error getting karma between dates: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<OASISResult<List<IKarmaAkashicRecord>>> GetKarmaAboveAsync(int karmaLevel)
        {
            try
            {
                if (!STARCLI.IsSTARIgnited)
                {
                    return new OASISResult<List<IKarmaAkashicRecord>>
                    {
                        IsError = true,
                        Message = "STAR is not ignited. Please ignite STAR first."
                    };
                }

                return await STARCLI.Avatars.GetKarmaAboveAsync(karmaLevel);
            }
            catch (Exception ex)
            {
                return new OASISResult<List<IKarmaAkashicRecord>>
                {
                    IsError = true,
                    Message = $"Error getting karma above level: {ex.Message}",
                    Exception = ex
                };
            }
        }

        public async Task<OASISResult<List<IKarmaAkashicRecord>>> GetKarmaBelowAsync(int karmaLevel)
        {
            try
            {
                if (!STARCLI.IsSTARIgnited)
                {
                    return new OASISResult<List<IKarmaAkashicRecord>>
                    {
                        IsError = true,
                        Message = "STAR is not ignited. Please ignite STAR first."
                    };
                }

                return await STARCLI.Avatars.GetKarmaBelowAsync(karmaLevel);
            }
            catch (Exception ex)
            {
                return new OASISResult<List<IKarmaAkashicRecord>>
                {
                    IsError = true,
                    Message = $"Error getting karma below level: {ex.Message}",
                    Exception = ex
                };
            }
        }
    }
}
