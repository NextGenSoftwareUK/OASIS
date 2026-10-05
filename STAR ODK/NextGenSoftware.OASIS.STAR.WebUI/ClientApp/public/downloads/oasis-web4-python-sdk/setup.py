from setuptools import setup, find_packages

with open("README.md", "r", encoding="utf-8") as fh:
    long_description = fh.read()

setup(
    name="oasis-web4-client",
    version="1.0.0",
    author="NextGen Software",
    author_email="support@nextgensoftware.co.uk",
    description="Official Python client for the OASIS Web4 API - Decentralized avatar management, karma system, and cross-provider data storage",
    long_description=long_description,
    long_description_content_type="text/markdown",
    url="https://github.com/NextGenSoftwareUK/OASIS-API",
    project_urls={
        "Bug Tracker": "https://github.com/NextGenSoftwareUK/OASIS-API/issues",
        "Documentation": "https://docs.oasis.network/web4-api",
        "Source Code": "https://github.com/NextGenSoftwareUK/OASIS-API",
    },
    classifiers=[
        "Development Status :: 5 - Production/Stable",
        "Intended Audience :: Developers",
        "Topic :: Software Development :: Libraries :: Python Modules",
        "License :: OSI Approved :: MIT License",
        "Programming Language :: Python :: 3",
        "Programming Language :: Python :: 3.8",
        "Programming Language :: Python :: 3.9",
        "Programming Language :: Python :: 3.10",
        "Programming Language :: Python :: 3.11",
        "Programming Language :: Python :: 3.12",
    ],
    package_dir={"": "src"},
    packages=find_packages(where="src"),
    python_requires=">=3.8",
    install_requires=[
        "requests>=2.28.0",
        "pydantic>=2.0.0",
        "typing-extensions>=4.5.0",
    ],
    extras_require={
        "dev": [
            "pytest>=7.0.0",
            "pytest-asyncio>=0.21.0",
            "black>=23.0.0",
            "mypy>=1.0.0",
        ],
    },
    keywords="oasis web4 decentralized blockchain avatar karma nft",
)
