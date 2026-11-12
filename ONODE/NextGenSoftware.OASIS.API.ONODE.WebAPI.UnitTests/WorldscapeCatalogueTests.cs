using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Places;

public sealed class WorldscapeCatalogueTests
{
    [Fact]
    public void CatalogueKeepsRealIdsCoordinatesAndCategoryAndDeduplicates()
    {
        const string json="{\"elements\":[{\"type\":\"node\",\"id\":17,\"lat\":21.1,\"lon\":-86.7,\"tags\":{\"name\":\"Owned Studio\",\"sport\":\"yoga\"}},{\"type\":\"node\",\"id\":17,\"lat\":21.1,\"lon\":-86.7,\"tags\":{\"name\":\"Owned Studio\",\"sport\":\"yoga\"}},{\"type\":\"way\",\"id\":18,\"center\":{\"lat\":21.2,\"lon\":-86.8},\"tags\":{\"name\":\"Owned Café\",\"diet:vegan\":\"only\"}}]}";
        var places=WorldscapePlaceCatalogue.Parse(json,"All");Assert.Equal(2,places.Length);
        Assert.Equal("osm:node:17",places[0].Id);Assert.Equal("Yoga",places[0].Category);Assert.Equal(21.1,places[0].Latitude);
        Assert.Equal("Vegan Food",places[1].Category);Assert.Equal(-86.8,places[1].Longitude);
    }
    [Fact]
    public void PartialProviderFailureDoesNotBecomeAnEmptySuccess()
        => Assert.Throws<InvalidOperationException>(()=>WorldscapePlaceCatalogue.Parse("{\"remark\":\"timeout\",\"elements\":[]}","Yoga"));
    [Theory]
    [InlineData(double.NaN,0)] [InlineData(91,0)] [InlineData(0,181)]
    public void InvalidLocationIsRejected(double latitude,double longitude)
        => Assert.Throws<ArgumentException>(()=>WorldscapePlaceCatalogue.ValidateCoordinates(latitude,longitude));
    [Fact]
    public void DistanceIsInMetres()
        => Assert.InRange(WorldscapePlaceCatalogue.Distance(0,0,0,.001),111,112);
    [Fact]
    public void NatureAreaPresenceUsesItsBoundaryRatherThanOnlyItsCentre()
    {
        var place=new WorldscapePlace { Latitude=21,Longitude=-86,Boundary=new[]{new[]{20.99,-86.01},new[]{20.99,-85.99},new[]{21.01,-85.99},new[]{21.01,-86.01},new[]{20.99,-86.01}}};
        Assert.Equal(0,WorldscapePlaceCatalogue.DistanceToPlace(place,21.005,-86));
        Assert.True(WorldscapePlaceCatalogue.DistanceToPlace(place,21.03,-86)>100);
    }
    [Fact]
    public void GeoapifyUsesStableOsmIdentityAndDoesNotInventMissingHoursOrRatings()
    {
        const string json="{\"features\":[{\"geometry\":{\"coordinates\":[-86.7,21.1]},\"properties\":{\"name\":\"Owned Studio\",\"place_id\":\"fixture\",\"datasource\":{\"raw\":{\"osm_id\":17,\"osm_type\":\"n\",\"sport\":\"yoga\",\"name\":\"Owned Studio\"}}}}]}";
        var row=Assert.Single(GeoapifyPlaceSource.Parse(json,"Yoga"));
        Assert.Equal("osm:node:17",row.Id);Assert.Equal("Yoga",row.Category);Assert.Null(row.OpeningHours);Assert.Equal("Geoapify / OpenStreetMap",row.Source);
    }}
