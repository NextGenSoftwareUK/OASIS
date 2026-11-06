# Weaviate native protocol

The protocol files and license are unmodified from Weaviate.Client 1.2.0
source commit `90ffbe6634db13a8c8bfbce963e2f9c3f437c3f1`. The upstream
directory is `src/Weaviate.Client/gRPC/proto/v1`. The copy was verified
against each upstream Git blob. [Pinned source](https://github.com/weaviate/csharp-client/tree/90ffbe6634db13a8c8bfbce963e2f9c3f437c3f1/src/Weaviate.Client/gRPC/proto/v1).

The provider generates its own internal client so the query-only host can
supply the channel, TLS transport and wire limits. The high-level SDK
continues to own the existing provider path.
