"""Data models for OASIS Web4 API"""

from typing import Optional, Dict, List, Any, Generic, TypeVar
from pydantic import BaseModel, Field

T = TypeVar('T')


class OASISResult(BaseModel, Generic[T]):
    """OASIS API result wrapper"""
    is_error: bool = Field(alias="isError")
    message: str
    result: Optional[T] = None
    is_saved: Optional[bool] = Field(None, alias="isSaved")
    
    class Config:
        populate_by_name = True


class Avatar(BaseModel):
    """Avatar model"""
    id: str
    username: str
    email: str
    first_name: Optional[str] = Field(None, alias="firstName")
    last_name: Optional[str] = Field(None, alias="lastName")
    image: Optional[str] = None
    bio: Optional[str] = None
    karma: Optional[int] = None
    level: Optional[int] = None
    created_date: Optional[str] = Field(None, alias="createdDate")
    last_login_date: Optional[str] = Field(None, alias="lastLoginDate")
    provider_key: Optional[Dict[str, str]] = Field(None, alias="providerKey")
    
    class Config:
        populate_by_name = True


class CreateAvatarRequest(BaseModel):
    """Request to create a new avatar"""
    username: str
    email: str
    password: str
    first_name: Optional[str] = Field(None, alias="firstName")
    last_name: Optional[str] = Field(None, alias="lastName")
    accept_terms: bool = Field(alias="acceptTerms")
    
    class Config:
        populate_by_name = True


class Karma(BaseModel):
    """Karma model"""
    total: int
    rank: Optional[int] = None
    level: Optional[int] = None
    next_level_at: Optional[int] = Field(None, alias="nextLevelAt")
    history: Optional[List[Dict[str, Any]]] = None
    
    class Config:
        populate_by_name = True


class AddKarmaRequest(BaseModel):
    """Request to add karma"""
    amount: int
    reason: str
    karma_type: Optional[str] = Field(None, alias="karmaType")
    karma_source_type: Optional[str] = Field(None, alias="karmaSourceType")
    
    class Config:
        populate_by_name = True


class NFT(BaseModel):
    """NFT model"""
    id: str
    name: str
    description: str
    image_url: str = Field(alias="imageUrl")
    collection: Optional[str] = None
    price: Optional[float] = None
    owner: str
    metadata: Optional[Dict[str, Any]] = None
    blockchain: Optional[str] = None
    token_id: Optional[str] = Field(None, alias="tokenId")
    created_date: Optional[str] = Field(None, alias="createdDate")
    
    class Config:
        populate_by_name = True


class Provider(BaseModel):
    """Provider model"""
    name: str
    description: str
    icon: Optional[str] = None
    is_active: bool = Field(alias="isActive")
    is_available: bool = Field(alias="isAvailable")
    
    class Config:
        populate_by_name = True
