

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    public enum CoordinatorSource
    {
        Path,
        Bundle //TODO: Implement CoordinatorBundle. https://docs.rs/holochain_types/0.2.1/holochain_types/dna/struct.CoordinatorBundle.html
    }
}


/*
pub enum CoordinatorSource {
    Path(PathBuf),
    Bundle(Box<CoordinatorBundle>),
}
*/