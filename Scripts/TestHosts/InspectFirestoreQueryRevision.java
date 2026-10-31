import java.lang.reflect.Method;
import java.util.List;
import java.util.Map;
import com.google.firestore.v1.DeleteDocumentRequest;
import com.google.protobuf.Descriptors;
import com.google.protobuf.Message;

// Read-only diagnostic against the retained official emulator's actual transcoder.
// Run with java --class-path <retained-jar> Scripts/TestHosts/InspectFirestoreQueryRevision.java.
class InspectFirestoreQueryRevision {
    public static void main(String[] args) throws Exception {
        Class<?> adapter = Class.forName("io.gapi.emulators.grpc.HttpAdapter");
        Method query = adapter.getDeclaredMethod("getQueryParams", String.class);
        Method merge = adapter.getDeclaredMethod("mergeFields", Map.class, Descriptors.Descriptor.class, Message.Builder.class);
        query.setAccessible(true);
        merge.setAccessible(true);
        for (String key : List.of("currentDocument.updateTime", "current_document.update_time")) {
            DeleteDocumentRequest.Builder request = DeleteDocumentRequest.newBuilder();
            merge.invoke(null, query.invoke(null, key + "=2026-10-19T16:51:35.325224Z"), request.getDescriptorForType(), request);
            var revision = request.getCurrentDocument().getUpdateTime();
            System.out.println(key + " => hasCondition=" + request.hasCurrentDocument()
                + ", hasRevision=" + request.getCurrentDocument().hasUpdateTime()
                + ", seconds=" + revision.getSeconds() + ", nanos=" + revision.getNanos());
            if(revision.getSeconds()!=0 || revision.getNanos()!=0) throw new IllegalStateException("Retained transcoder behavior changed; review diagnostic.");
        }
        var body = DeleteDocumentRequest.newBuilder();
        io.gapi.emulators.grpc.JsonFormat.parser().merge("{\"currentDocument\":{\"updateTime\":\"2026-10-19T16:51:35.325224Z\"}}",body);
        var revision=body.getCurrentDocument().getUpdateTime();
        if(revision.getSeconds()!=1792428695L || revision.getNanos()!=325224000) throw new IllegalStateException("Body timestamp not preserved.");
        System.out.println("JSON body => exact seconds="+revision.getSeconds()+", nanos="+revision.getNanos());
    }
}
