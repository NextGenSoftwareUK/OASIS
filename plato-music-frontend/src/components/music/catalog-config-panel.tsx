"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { MusicTrack } from "@/types/music";

interface CatalogConfigPanelProps {
  selectedOption?: string;
  onSelect: (option: string) => void;
  track?: MusicTrack;
  onTrackChange?: (track: MusicTrack) => void;
}

const MUSIC_GENRES = [
  "Pop", "Rock", "Hip-Hop", "R&B", "Electronic", "Country", "Jazz", "Classical",
  "Folk", "Reggae", "Blues", "Alternative", "Indie", "Funk", "Soul", "Gospel",
  "Latin", "World", "Ambient", "Experimental", "Other"
];

const MUSIC_KEYS = [
  "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"
];

const MOOD_OPTIONS = [
  "Happy", "Sad", "Energetic", "Calm", "Romantic", "Aggressive", "Melancholic",
  "Uplifting", "Dark", "Playful", "Intense", "Peaceful", "Nostalgic", "Mysterious"
];

export function CatalogConfigPanel({ selectedOption, onSelect, track, onTrackChange }: CatalogConfigPanelProps) {
  const [localTrack, setLocalTrack] = useState<MusicTrack>(track || {
    id: "",
    title: "",
    artist: "",
    album: "",
    isrc: "",
    genre: "",
    duration: 0,
    bpm: undefined,
    key: undefined,
    mood: [],
    releaseDate: new Date()
  });

  const updateTrack = (updates: Partial<MusicTrack>) => {
    const newTrack = { ...localTrack, ...updates };
    setLocalTrack(newTrack);
    onTrackChange?.(newTrack);
  };

  const toggleMood = (mood: string) => {
    const currentMoods = localTrack.mood || [];
    const newMoods = currentMoods.includes(mood)
      ? currentMoods.filter(m => m !== mood)
      : [...currentMoods, mood];
    updateTrack({ mood: newMoods });
  };

  const CONFIG_OPTIONS = [
    {
      id: "single-track",
      title: "Single Track",
      description: "Tokenize a single track with custom royalty splits and micro-licensing",
      features: ["Custom royalty splits", "Micro-licensing setup", "Single track metadata"]
    },
    {
      id: "album-collection",
      title: "Album Collection",
      description: "Tokenize an entire album with consistent royalty structure across tracks",
      features: ["Bulk track upload", "Consistent royalty splits", "Album-level metadata"]
    },
    {
      id: "label-catalog",
      title: "Label Catalog",
      description: "Tokenize multiple albums and tracks with label-wide licensing templates",
      features: ["Multi-album support", "Label templates", "Bulk management"]
    }
  ];

  return (
    <div className="space-y-6">
      <div>
        <h3 className="text-xl font-semibold text-[var(--color-foreground)]">Music Catalog Configuration</h3>
        <p className="mt-2 text-sm text-[var(--muted)]">
          Choose how you want to structure your music tokenization
        </p>
      </div>

      <div className="grid gap-4">
        {CONFIG_OPTIONS.map((option) => (
          <div
            key={option.id}
            className={cn(
              "rounded-xl border p-4 cursor-pointer transition-all",
              selectedOption === option.id
                ? "border-[var(--accent)]/70 bg-[rgba(34,211,238,0.12)]"
                : "border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.7)] hover:border-[var(--accent)]/30"
            )}
            onClick={() => onSelect(option.id)}
          >
            <div className="flex items-start justify-between">
              <div className="flex-1">
                <h4 className="font-semibold text-[var(--color-foreground)]">{option.title}</h4>
                <p className="mt-1 text-sm text-[var(--muted)]">{option.description}</p>
                <ul className="mt-2 flex flex-wrap gap-1">
                  {option.features.map((feature, index) => (
                    <li
                      key={index}
                      className="inline-flex items-center rounded-full border border-[var(--color-card-border)]/40 bg-[rgba(8,11,26,0.6)] px-2 py-1 text-xs text-[var(--muted)]"
                    >
                      {feature}
                    </li>
                  ))}
                </ul>
              </div>
              <div className="ml-4 flex items-center">
                {selectedOption === option.id && (
                  <div className="flex h-5 w-5 items-center justify-center rounded-full bg-[var(--accent)]">
                    <svg className="h-3 w-3 text-[#041321]" fill="currentColor" viewBox="0 0 20 20">
                      <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                    </svg>
                  </div>
                )}
              </div>
            </div>
          </div>
        ))}
      </div>

      {selectedOption && (
        <div className="rounded-xl border border-[var(--color-card-border)]/60 bg-[rgba(8,12,28,0.85)] p-6">
          <h4 className="text-lg font-semibold text-[var(--color-foreground)] mb-4">Track Information</h4>
          
          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <label className="block text-sm font-medium text-[var(--color-foreground)] mb-2">
                Track Title *
              </label>
              <input
                type="text"
                value={localTrack.title}
                onChange={(e) => updateTrack({ title: e.target.value })}
                placeholder="Enter track title"
                className="w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-3 py-2 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-[var(--color-foreground)] mb-2">
                Artist *
              </label>
              <input
                type="text"
                value={localTrack.artist}
                onChange={(e) => updateTrack({ artist: e.target.value })}
                placeholder="Enter artist name"
                className="w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-3 py-2 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-[var(--color-foreground)] mb-2">
                Album
              </label>
              <input
                type="text"
                value={localTrack.album || ""}
                onChange={(e) => updateTrack({ album: e.target.value })}
                placeholder="Enter album name"
                className="w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-3 py-2 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-[var(--color-foreground)] mb-2">
                ISRC Code
              </label>
              <input
                type="text"
                value={localTrack.isrc}
                onChange={(e) => updateTrack({ isrc: e.target.value })}
                placeholder="e.g., USRC17607839"
                className="w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-3 py-2 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-[var(--color-foreground)] mb-2">
                Genre *
              </label>
              <select
                value={localTrack.genre}
                onChange={(e) => updateTrack({ genre: e.target.value })}
                className="w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-3 py-2 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none"
              >
                <option value="">Select genre...</option>
                {MUSIC_GENRES.map(genre => (
                  <option key={genre} value={genre}>{genre}</option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-[var(--color-foreground)] mb-2">
                Duration (seconds)
              </label>
              <input
                type="number"
                value={localTrack.duration}
                onChange={(e) => updateTrack({ duration: Number(e.target.value) })}
                min="0"
                placeholder="180"
                className="w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-3 py-2 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-[var(--color-foreground)] mb-2">
                BPM
              </label>
              <input
                type="number"
                value={localTrack.bpm || ""}
                onChange={(e) => updateTrack({ bpm: Number(e.target.value) || undefined })}
                min="0"
                max="300"
                placeholder="120"
                className="w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-3 py-2 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-[var(--color-foreground)] mb-2">
                Key
              </label>
              <select
                value={localTrack.key || ""}
                onChange={(e) => updateTrack({ key: e.target.value || undefined })}
                className="w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-3 py-2 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none"
              >
                <option value="">Select key...</option>
                {MUSIC_KEYS.map(key => (
                  <option key={key} value={key}>{key}</option>
                ))}
              </select>
            </div>
          </div>

          <div className="mt-4">
            <label className="block text-sm font-medium text-[var(--color-foreground)] mb-2">
              Mood Tags
            </label>
            <div className="flex flex-wrap gap-2">
              {MOOD_OPTIONS.map(mood => (
                <button
                  key={mood}
                  onClick={() => toggleMood(mood)}
                  className={cn(
                    "rounded-full border px-3 py-1 text-xs font-medium transition-colors",
                    localTrack.mood?.includes(mood)
                      ? "border-[var(--accent)]/70 bg-[rgba(34,211,238,0.12)] text-[var(--accent)]"
                      : "border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.7)] text-[var(--muted)] hover:text-[var(--color-foreground)]"
                  )}
                >
                  {mood}
                </button>
              ))}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}



