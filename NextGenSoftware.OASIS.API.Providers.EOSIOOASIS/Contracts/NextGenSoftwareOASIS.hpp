#include <eosio/eosio.hpp>

using namespace std;
using namespace eosio;

namespace NextGenSoftwareOASIS
{
    class [[eosio::contract("OASISRepository")]] OASISRepository : public contract
    {
        public:
            [[eosio::action]]
            void CreateAvatar(long entityId, string avatarId, string info);
            [[eosio::action]]
            void ReadAvatar(long entityId);
            [[eosio::action]]
            void ReadAllAvatars();
            [[eosio::action]]
            void UpdateAvatar(long entityId, string info);
            [[eosio::action]]
            void HardDeleteAvatar(long entityId);
            [[eosio::action]]
            void SoftDeleteAvatar(long entityId);

            [[eosio::action]]
            void CreateAvatarDetail(long entityId, string avatarId, string info);
            [[eosio::action]]
            void ReadAvatarDetail(long entityId);
            [[eosio::action]]
            void ReadAllAvatarDetails();

            [[eosio::action]]
            void CreateHolon(long entityId, string holonId, string info);
            [[eosio::action]]
            void ReadHolon(long entityId);
            [[eosio::action]]
            void ReadAllHolon();
            [[eosio::action]]
            void UpdateHolon(long entityId, string info);
            [[eosio::action]]
            void HardDeleteHolon(long entityId);
            [[eosio::action]]
            void SoftDeleteHolon(long entityId);

        private:
            struct [[eosio::table]] Avatar
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
            };

            struct [[eosio::table]] AvatarDetail
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
            };

            struct [[eosio::table]] Holon
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
            };

            using Avatars = multi_index<"Avatars"_n, Avatar>;
            using AvatarDetails = multi_index<"AvatarDetails"_n, AvatarDetail>;
            using Holons = multi_index<"Holons"_n, Holon>;
    };
}