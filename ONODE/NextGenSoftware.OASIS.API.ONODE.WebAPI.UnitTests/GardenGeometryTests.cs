using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Places;

public sealed class GardenGeometryTests
{
    [Fact] public void RefillMeasuresDistanceToTheStreamRatherThanItsCentre()
    {
        var stream=new WorldscapePlace{Latitude=21,Longitude=-86,Boundary=new[]{new[]{21d,-86d},new[]{21d,-85.98d}}};
        Assert.InRange(WorldscapePlaceCatalogue.DistanceToPlace(stream,21,-85.985),0,.01);
        Assert.InRange(WorldscapePlaceCatalogue.DistanceToPlace(stream,21.001,-85.985),110,112);
    }
    [Fact] public void ParkHolesDoNotCountAsPlantingGround()
    {
        double[][] Ring(double a,double b)=>new[]{new[]{a,a},new[]{a,b},new[]{b,b},new[]{b,a},new[]{a,a}};
        var park=new WorldscapePlace{OuterBoundaries=new[]{Ring(0,1)},InnerBoundaries=new[]{Ring(.4,.6)}};
        Assert.Equal(0,WorldscapePlaceCatalogue.DistanceToPlace(park,.2,.2));
        Assert.True(WorldscapePlaceCatalogue.DistanceToPlace(park,.5,.5)>10000);
    }
    [Fact] public void OsmWayUsesItsOrderedNodesWithoutInventingACentrePolygon()
    {
        const string json="{\"elements\":[{\"type\":\"node\",\"id\":1,\"lat\":21,\"lon\":-86},{\"type\":\"node\",\"id\":2,\"lat\":21,\"lon\":-85},{\"type\":\"way\",\"id\":17,\"nodes\":[2,1]}]}";
        var geometry=OsmPlaceGeometry.Parse(json,"way",17);Assert.Empty(geometry.Inner);
        Assert.Equal(-85,geometry.Outer[0][0][1]);Assert.Equal(-86,geometry.Outer[0][1][1]);
    }
}
