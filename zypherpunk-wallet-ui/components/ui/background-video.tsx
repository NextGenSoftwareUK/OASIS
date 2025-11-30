export default function BackgroundVideo() {
  return (
    <div className="fixed inset-0 -z-10">
      <video
        autoPlay
        muted
        loop
        playsInline
        className="w-full h-full object-cover"
        style={{ zIndex: -9999 }}
        onError={(e) => {
          console.log('Video failed to load, falling back to gradient');
          const target = e.target as HTMLVideoElement;
          target.style.display = 'none';
        }}
      >
        <source src="/background-video.mp4" type="video/mp4" />
        Your browser does not support the video tag.
      </video>
      {/* Overlay for better text readability */}
      <div className="absolute inset-0 bg-black/40"></div>
      {/* Fallback gradient if video fails */}
      <div className="absolute inset-0 cosmic-gradient opacity-0" id="fallback-gradient"></div>
    </div>
  );
} 