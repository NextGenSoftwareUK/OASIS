import { Fragment, useEffect, useMemo, useState } from "react";
import type { ReactNode } from "react";
import YAML from "yaml";
import deckSource from "#deck-schema?raw";
import "./App.css";

type Primitive = string | number | boolean | null;

type Slide = {
  id: number;
  layout: string;
  title?: string;
  headline?: string;
  [key: string]: unknown;
};

type DeckSchema = {
  meta?: {
    description?: string;
    [key: string]: unknown;
  };
  slides: Slide[];
};

const formatKey = (key: string): string =>
  key
    .replace(/_/g, " ")
    .replace(/\b\w/g, (match) => match.toUpperCase());

const renderPrimitive = (value: Primitive): ReactNode => {
  if (value === null || value === undefined) {
    return <span className="muted">—</span>;
  }

  const stringValue = String(value);
  if (stringValue.trim().length === 0) {
    return <span className="muted">—</span>;
  }

  return (
    <>
      {stringValue.split(/\n{2,}/).map((paragraph, idx) => (
        <p key={idx}>{paragraph.trim()}</p>
      ))}
    </>
  );
};

const renderObject = (value: Record<string, unknown>): ReactNode => (
  <div className="object-grid">
    {Object.entries(value).map(([key, nested]) => (
      <div className="object-field" key={key}>
        <span className="field-label">{formatKey(key)}</span>
        <div className="field-value">{renderValue(nested)}</div>
      </div>
    ))}
  </div>
);

const renderArray = (value: unknown[]): ReactNode => {
  if (value.length === 0) {
    return <span className="muted">No entries</span>;
  }

  const isPrimitiveArray = value.every(
    (item) =>
      typeof item === "string" ||
      typeof item === "number" ||
      typeof item === "boolean"
  );

  if (isPrimitiveArray) {
    return (
      <ul className="bullet-list">
        {value.map((item, idx) => (
          <li key={idx}>{renderPrimitive(item as Primitive)}</li>
        ))}
      </ul>
    );
  }

  return (
    <div className="object-card-list">
      {value.map((item, idx) => (
        <div className="object-card" key={idx}>
          {typeof item === "object" && item !== null
            ? renderObject(item as Record<string, unknown>)
            : renderPrimitive(item as Primitive)}
        </div>
      ))}
    </div>
  );
};

const renderValue = (value: unknown): ReactNode => {
  if (
    typeof value === "string" ||
    typeof value === "number" ||
    typeof value === "boolean" ||
    value === null
  ) {
    return renderPrimitive(value);
  }

  if (Array.isArray(value)) {
    return renderArray(value);
  }

  if (typeof value === "object" && value !== null) {
    return renderObject(value as Record<string, unknown>);
  }

  return <span className="muted">Unsupported content</span>;
};

const getSlideTitle = (slide: Slide): string =>
  slide.title || slide.headline || `Slide ${slide.id}`;

function App() {
  const schema = useMemo<DeckSchema>(() => {
    try {
      return (YAML.parse(deckSource) as DeckSchema) ?? { slides: [] };
    } catch (error) {
      console.error("Failed to parse deck schema", error);
      return { slides: [] };
    }
  }, [deckSource]);

  const slides = schema.slides ?? [];
  const [selectedIndex, setSelectedIndex] = useState(0);
  const [showRaw, setShowRaw] = useState(false);

  useEffect(() => {
    if (selectedIndex >= slides.length) {
      setSelectedIndex(0);
    }
  }, [slides.length, selectedIndex]);

  if (!slides.length) {
    return (
      <div className="app-shell">
        <main className="slide-viewer empty">
          <h1>No slides found</h1>
          <p>
            Update <code>timo_rides_new_deck.schema.yaml</code> to add content.
          </p>
        </main>
      </div>
    );
  }

  const currentSlide = slides[selectedIndex];
  const detailKeys = Object.keys(currentSlide).filter(
    (key) => !["id", "layout", "title", "headline"].includes(key)
  );

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="sidebar-header">
          <h1>TimoRides Deck</h1>
          {schema.meta?.description && (
            <p className="muted">{schema.meta.description}</p>
          )}
          <p className="meta-summary">
            {slides.length} slide{slides.length === 1 ? "" : "s"} · Layouts:{" "}
            {new Set(slides.map((slide) => slide.layout)).size}
          </p>
        </div>
        <div className="slide-list">
          {slides.map((slide, idx) => (
            <button
              key={slide.id ?? idx}
              className={`slide-button ${
                idx === selectedIndex ? "active" : ""
              }`}
              onClick={() => {
                setSelectedIndex(idx);
              }}
            >
              <span className="slide-number">
                {String(idx + 1).padStart(2, "0")}
              </span>
              <span className="slide-title">{getSlideTitle(slide)}</span>
              <span className="layout-tag">{slide.layout}</span>
            </button>
          ))}
        </div>
      </aside>
      <main className="slide-viewer">
        <header className="slide-header">
          <div>
            <p className="slide-count">
              Slide {selectedIndex + 1} of {slides.length}
            </p>
            <h2>{getSlideTitle(currentSlide)}</h2>
            {currentSlide.headline && currentSlide.title && (
              <p className="slide-headline">{currentSlide.headline}</p>
            )}
          </div>
          <div className="header-actions">
            <span className="layout-badge">{currentSlide.layout}</span>
            <button
              className="toggle-raw"
              type="button"
              onClick={() => setShowRaw((prev) => !prev)}
            >
              {showRaw ? "Formatted view" : "Show raw JSON"}
            </button>
          </div>
        </header>
        <section className="slide-content">
          {showRaw ? (
            <pre className="raw-preview">
              {JSON.stringify(currentSlide, null, 2)}
            </pre>
          ) : (
            <div className="content-sections">
              {!currentSlide.title && currentSlide.headline && (
                <div className="section">
                  <h3>Headline</h3>
                  {renderValue(currentSlide.headline)}
                </div>
              )}
              {detailKeys.map((key) => (
                <div className="section" key={key}>
                  <h3>{formatKey(key)}</h3>
                  {renderValue(currentSlide[key])}
                </div>
              ))}
            </div>
          )}
        </section>
        {schema.meta && (
          <footer className="deck-footer">
            {Object.entries(schema.meta)
              .filter(([key]) => !["description"].includes(key))
              .map(([key, value]) => (
                <Fragment key={key}>
                  <span className="footer-label">{formatKey(key)}</span>
                  <span className="footer-value">
                    {(() => {
                      if (
                        typeof value === "string" ||
                        typeof value === "number"
                      ) {
                        return value;
                      }
                      return JSON.stringify(value);
                    })()}
                  </span>
                </Fragment>
              ))}
          </footer>
        )}
      </main>
    </div>
  );
}

export default App;
