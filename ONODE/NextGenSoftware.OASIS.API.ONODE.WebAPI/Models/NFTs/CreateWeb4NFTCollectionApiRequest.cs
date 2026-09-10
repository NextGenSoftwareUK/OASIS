using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.NFT
{
    /// <summary>
    /// Request body for POST api/nft/create-web4-nft-collection.
    /// <para>
    /// The endpoint previously bound straight to ICreateWeb4NFTCollectionRequest. ASP.NET Core cannot
    /// instantiate an interface, so every call threw during model binding and came back as the generic
    /// "Oooops. Sorry something broke" handler. This concrete model binds cleanly and is mapped onto the
    /// core request in the controller.
    /// </para>
    /// <para>
    /// Collection members are referenced by id (Web4NFTIds) rather than as full NFT objects - the core
    /// request's List&lt;IWeb4NFT&gt; is likewise not bindable from JSON, and the manager only reads the ids.
    /// </para>
    /// </summary>
    public class CreateWeb4NFTCollectionApiRequest
    {
        /// <summary>Collection title. Required.</summary>
        public string Title { get; set; }

        /// <summary>Optional description of the collection.</summary>
        public string Description { get; set; }

        /// <summary>Avatar id creating the collection. Defaults to the authenticated avatar when omitted.</summary>
        public Guid CreatedBy { get; set; }

        /// <summary>Optional raw collection image bytes.</summary>
        public byte[] Image { get; set; }

        /// <summary>Optional URL to the collection image.</summary>
        public string ImageUrl { get; set; }

        /// <summary>Optional raw thumbnail bytes.</summary>
        public byte[] Thumbnail { get; set; }

        /// <summary>Optional URL to the collection thumbnail.</summary>
        public string ThumbnailUrl { get; set; }

        /// <summary>Optional free-form metadata.</summary>
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();

        /// <summary>Ids of the Web4 NFTs that belong to this collection.</summary>
        public List<string> Web4NFTIds { get; set; } = new List<string>();

        /// <summary>Optional tags.</summary>
        public List<string> Tags { get; set; } = new List<string>();
    }
}
