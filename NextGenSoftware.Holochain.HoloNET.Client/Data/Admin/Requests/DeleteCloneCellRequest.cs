
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests
{
    [MessagePackObject]
    public class DeleteCloneCellRequest //Same as DisableCloneCellRequest on App API.
    {
        /// <summary>
        /// The app id that the clone cell belongs to
        /// </summary>
        [Key("app_id")]
        public string app_id { get; set; }

        /// <summary>
        ///  The clone id (string/rolename) or cell id (byte[][]) of the clone cell.
        /// </summary>
        [Key("clone_cell_id")]
        public dynamic clone_cell_id { get; set; }
    }
}


//export interface DisableCloneCellRequest
//{
//    /**
//     * The app id that the clone cell belongs to
//     */
//    app_id: InstalledAppId;
//  /**
//   * The clone id or cell id of the clone cell
//   */
//  clone_cell_id: RoleName | CellId; //RoleName is a string and CellId is a byte[][]
//}