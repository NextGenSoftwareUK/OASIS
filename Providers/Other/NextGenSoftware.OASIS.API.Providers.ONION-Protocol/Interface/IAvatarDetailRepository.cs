//using OASIS_Onion.Model.Interfaces;
//using System;
//using System.Collections.Generic;
//using System.Linq.Expressions;
//using System.Threading.Tasks;
//using AvatarDetail = OASIS_Onion.Model.Models.AvatarDetail;

//namespace OASIS_Onion.Repository.Interface
//{
//    public interface IAvatarDetailRepository : IEntityRepository<AvatarDetail>
//    {
//        AvatarDetail Add(AvatarDetail avatar);

//        Task<AvatarDetail> AddAsync(AvatarDetail avatar);

//        AvatarDetail GetAvatarDetail(Guid id);

//        AvatarDetail GetAvatarDetail(string username);

//        //AvatarDetail GetAvatarDetail(string username, string password);
//        Task<AvatarDetail> GetAvatarDetailAsync(Guid id);

//        Task<AvatarDetail> GetAvatarDetailAsync(string username);

//        Task<IEnumerable<AvatarDetail>> GetAvatarDetailsAsync();

//        IEnumerable<AvatarDetail> GetAvatarDetails();

//        AvatarDetail Update(AvatarDetail avatar);

//        Task<AvatarDetail> UpdateAsync(AvatarDetail avatar);

//        AvatarDetail GetAvatarDetail(Expression<Func<AvatarDetail, bool>> expression);

//        Task<AvatarDetail> GetAvatarDetailAsync(Expression<Func<AvatarDetail, bool>> expression);
//    }
//}