   cd C:\Source\OASIS\ONODE\NextGenSoftware.OASIS.API.ONODE.WebAPI
   dotnet run --urls http://localhost:50563

   cd "C:\Source\OASIS\STAR ODK\NextGenSoftware.OASIS.STAR.WebAPI"
   dotnet run --urls http://localhost:50564

   cd "C:\Source\OASIS\STAR ODK\NextGenSoftware.OASIS.STAR.WebUI\ClientApp"
   npm install
   set REACT_APP_API_URL=http://localhost:50564/api
   set REACT_APP_WEB4_API_URL=http://localhost:50563/api
   npm start