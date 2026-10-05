"""Exceptions for OASIS Web4 client"""


class OASISError(Exception):
    """Base exception for OASIS Web4 client"""
    pass


class OASISAPIError(OASISError):
    """API request failed"""
    pass


class OASISAuthError(OASISError):
    """Authentication failed"""
    pass


class OASISConfigError(OASISError):
    """Configuration error"""
    pass
