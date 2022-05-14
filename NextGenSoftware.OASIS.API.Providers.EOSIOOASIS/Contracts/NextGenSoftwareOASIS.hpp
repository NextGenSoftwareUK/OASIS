#include <eosio/eosio.hpp>
#include <eosio/singleton.hpp>
#include <eosio/asset.hpp>

using namespace std;
using namespace eosio;

namespace NextGenSoftwareOASIS
{
    class OASISRepository : public contract
    {
        public:
            ACTION CreateAvatar(long entityId, string avatarId, string info);
            ACTION ReadAvatar(long entityId);
            ACTION ReadAllAvatars();
            ACTION UpdateAvatar(long entityId, string info);
            ACTION HardDeleteAvatar(long entityId);
            ACTION SoftDeleteAvatar(long entityId);

            ACTION CreateAvatarDetail(long entityId, string avatarId, string info);
            ACTION ReadAvatarDetail(long entityId);
            ACTION ReadAllAvatarDetails();

            ACTION CreateHolon(long entityId, string holonId, string info);
            ACTION ReadHolon(long entityId);
            ACTION ReadAllHolon();
            ACTION UpdateHolon(long entityId, string info);
            ACTION HardDeleteHolon(long entityId);
            ACTION SoftDeleteHolon(long entityId);

        private:
            struct Avatar
            {
                long EntityId;
                string AvatarId;
                string Info;
                bool IsDeleted;

                long primary_key() const
                {
                    return EntityId;
                }

                string secondary_key() const
                {
                    return AvatarId;
                }

                EOSLIB_SERIALIZE(Avatar, (EntityId)(AvatarId)(Info)(IsDeleted));
            };

            struct AvatarDetail
            {
                long EntityId;
                string AvatarId;
                string Info;

                long primary_key() const
                {
                    return EntityId;
                }

                string secondary_key() const
                {
                    return AvatarId;
                }

                EOSLIB_SERIALIZE(AvatarDetail, (EntityId), (AvatarId), (Info));
            };

            struct Holon
            {
                long EntityId;
                string HolonId;
                string Info;
                bool IsDeleted;

                long primary_key() const
                {
                    return EntityId;
                }

                string secondary_key() const
                {
                    return HolonId;
                }

                EOSLIB_SERIALIZE(Holon, (EntityId), (HolonId), (Info), (IsDeleted));
            };

            typedef multi_index<N(Avatars), Avatar> Avatars;
            typedef multi_index<N(AvatarDetails), AvatarDetail> AvatarDetails;
            typedef multi_index<N(Holons), Holon> Holons;
    };

    EOSIO_ABI(OASISRepository, 
        (CreateAvatar)(ReadAvatar)(ReadAllAvatars)(UpdateAvatar)(HardDeleteAvatar)(SoftDeleteAvatar)
        (CreateAvatarDetail)(ReadAvatarDetail)(ReadAllAvatarDetails)
        (CreateHolon)(ReadHolon)(ReadAllHolon)(UpdateHolon)(HardDeleteHolon)(SoftDeleteHolon))
}