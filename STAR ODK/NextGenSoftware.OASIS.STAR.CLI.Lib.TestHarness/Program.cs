// See https://aka.ms/new-console-template for more information
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.STAR;
using NextGenSoftware.OASIS.STAR.CLI.Lib;

Console.WriteLine("Hello, World!");

STARCLI.Holons.ShowHolons(new List<IHolon> 
{ 
    new Holon() { Id = Guid.NewGuid(), Name = "Test Holon 1" }, 
    new Holon() { Id = Guid.NewGuid(), Name = "Test Holon 2" } 
});
