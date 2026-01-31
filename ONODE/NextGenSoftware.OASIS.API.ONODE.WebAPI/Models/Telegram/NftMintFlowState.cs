using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Telegram
{
    /// <summary>
    /// Current step in the Telegram NFT mint wizard.
    /// </summary>
    public enum NftMintFlowStep
    {
        None = 0,
        OasisLogin,
        OasisPassword,
        Image,
        Title,
        Symbol,
        Description,
        Wallet,
        Confirm,
        Done
    }

    /// <summary>
    /// Per-chat state for the NFT mint flow inside Telegram.
    /// </summary>
    public class NftMintFlowState
    {
        public long ChatId { get; set; }
        public long? UserId { get; set; }
        public NftMintFlowStep Step { get; set; }
        public string ImageUrl { get; set; }
        /// <summary>MIME type of uploaded image (e.g. image/jpeg, image/png) for metadata properties.</summary>
        public string ImageContentType { get; set; }
        public string Title { get; set; }
        public string Symbol { get; set; }
        public string Description { get; set; }
        public string SendToWalletAddress { get; set; }
        public System.Collections.Generic.Dictionary<string, object> MetaData { get; set; }
        /// <summary>OASIS avatar ID after successful login; null if user skipped login (mint as bot).</summary>
        public Guid? UserAvatarId { get; set; }
        /// <summary>Username entered for OASIS login (used in password step).</summary>
        public string OasisUsername { get; set; }
        /// <summary>When set (e.g. from memecoin import), use this as JSONMetaDataURL instead of uploading to Pinata.</summary>
        public string JsonMetaDataUrl { get; set; }
    }
}
