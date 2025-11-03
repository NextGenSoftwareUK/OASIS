namespace NextGenSoftware.OASIS.API.Core.Enums
{
    /// <summary>
    /// Represents the position of an egg in the avatar's trophy gallery
    /// </summary>
    public enum GalleryPosition
    {
        /// <summary>
        /// Not displayed in gallery
        /// </summary>
        Hidden = 0,
        
        /// <summary>
        /// Top row, left position
        /// </summary>
        TopLeft = 1,
        
        /// <summary>
        /// Top row, center position
        /// </summary>
        TopCenter = 2,
        
        /// <summary>
        /// Top row, right position
        /// </summary>
        TopRight = 3,
        
        /// <summary>
        /// Middle row, left position
        /// </summary>
        MiddleLeft = 4,
        
        /// <summary>
        /// Middle row, center position (most prominent)
        /// </summary>
        MiddleCenter = 5,
        
        /// <summary>
        /// Middle row, right position
        /// </summary>
        MiddleRight = 6,
        
        /// <summary>
        /// Bottom row, left position
        /// </summary>
        BottomLeft = 7,
        
        /// <summary>
        /// Bottom row, center position
        /// </summary>
        BottomCenter = 8,
        
        /// <summary>
        /// Bottom row, right position
        /// </summary>
        BottomRight = 9,
        
        /// <summary>
        /// Special position for featured eggs
        /// </summary>
        Featured = 10,
        
        /// <summary>
        /// Special position for legendary eggs
        /// </summary>
        Legendary = 11,
        
        /// <summary>
        /// Special position for divine eggs
        /// </summary>
        Divine = 12
    }
}
