using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class Hematite : Crystal
    {
        public Hematite()
        {
            this.Name = new EnumValue<CrystalName>(CrystalName.Hematite);
            this.Type = new EnumValue<CrystalType>(CrystalType.Grounding);
            this.Description = "If you're drawn to the Hematite crystal, it could be a sign that you're in need of grounding and balancing in your life. As soon as the Hematite crystal stone touches the skin, you'll feel more centered and calm with its intense but subtle vibrations. While all crystals have powerful grounding effects, the Hematite crystal properties are a gem when it comes to clearing and activating the root chakra, the energy center that anchors us to the earth and provides a feeling of stability. The Hematite crystal healing properties stem from the stone’s high iron content, which gives it a heaviness and a weightiness that gives you the feeling of being anchored to the earth. With the simple act of bringing you back down to earth, Hematite calms a troubled mind and puts your spirit at ease so you can release any feelings of stress, tension, worry, or nervousness. The stabilizing effect of the Hematite crystal stone makes it a trusted companion to keep by your side everywhere you go.";
            this.GroundingLevel = 99;
        }
    }
}