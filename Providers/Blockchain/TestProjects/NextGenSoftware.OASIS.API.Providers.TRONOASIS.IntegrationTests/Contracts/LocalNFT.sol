// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

// Local integration fixture. Deliberately non-enumerable, with explicit mint/burn extensions.
contract LocalNFT {
    address public immutable minter = msg.sender;
    mapping(uint256 => address) public ownerOf;
    mapping(address => uint256) public balanceOf;
    mapping(uint256 => string) private uris;
    mapping(uint256 => address) public getApproved;
    event Transfer(address indexed from, address indexed to, uint256 indexed tokenId);
    event Approval(address indexed owner, address indexed approved, uint256 indexed tokenId);
    function approve(address approved, uint256 id) external {
        address owner = ownerOf[id];
        require(owner != address(0) && msg.sender == owner, "Invalid approval");
        getApproved[id] = approved; emit Approval(owner, approved, id);
    }
    function tokenURI(uint256 id) external view returns (string memory) {
        require(ownerOf[id] != address(0), "Missing NFT"); return uris[id];
    }
    function mint(address to, uint256 id, string calldata uri) external {
        require(msg.sender == minter && to != address(0) && ownerOf[id] == address(0), "Invalid mint");
        ownerOf[id] = to; balanceOf[to]++; uris[id] = uri; emit Transfer(address(0), to, id);
    }
    function safeTransferFrom(address from, address to, uint256 id) external {
        transfer(from, to, id);
        if (to.code.length != 0) {
            (bool ok, bytes memory result) = to.call(abi.encodeWithSignature("onERC721Received(address,address,uint256,bytes)", msg.sender, from, id, ""));
            require(ok && abi.decode(result, (bytes4)) == bytes4(keccak256("onERC721Received(address,address,uint256,bytes)")), "Unsafe recipient");
        }
    }
    function transferFrom(address from, address to, uint256 id) external {
        transfer(from, to, id);
    }
    function transfer(address from, address to, uint256 id) private {
        require(ownerOf[id] == from && (msg.sender == from || msg.sender == getApproved[id]) && to != address(0), "Invalid transfer");
        delete getApproved[id];
        ownerOf[id] = to; balanceOf[from]--; balanceOf[to]++;
        emit Transfer(from, to, id);
    }
    function burn(uint256 id) external {
        address owner = ownerOf[id]; require(owner != address(0) && msg.sender == owner, "Invalid burn");
        balanceOf[owner]--; delete ownerOf[id]; delete uris[id]; delete getApproved[id]; emit Transfer(owner, address(0), id);
    }
}
