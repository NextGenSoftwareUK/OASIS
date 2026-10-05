export default function jsonLDGenerator({ title, description, url, image }) {
  return `<script type="application/ld+json">
  {
    "@context": "https://schema.org",
    "@type": "Organization",
    "name": "${title}",
    "url": "${url}",
    "logo": "${url}${image.src}",
    "description": "${description}",
    "areaServed": "ZA"
  }
  </script>`;
}
