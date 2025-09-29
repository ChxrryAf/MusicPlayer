import { useState, useEffect, useRef } from 'react';
import { Play, Pause, SkipBack, SkipForward, Volume2, Heart, Shuffle, Repeat } from 'lucide-react';
import { Button } from './ui/button';
import { Slider } from './ui/slider';
import { ImageWithFallback } from './figma/ImageWithFallback';

interface Song {
  id: number;
  title: string;
  artist: string;
  duration: number;
  albumArt: string;   // bevorzugt absolute URL vom Backend
  streamUrl?: string; // bevorzugt absolute URL vom Backend
}

const API_BASE = 'http://localhost:5273';
const VOLUME_KEY = 'player.volume';

// ---- Fallback: sofort sichtbare UI ----
const fallbackCover1 = new URL('../assets/catja.jpg', import.meta.url).href;
const fallbackCover2 = new URL('../assets/cat2.jpg', import.meta.url).href;
const fallbackCover3 = new URL('../assets/AVO.jpg', import.meta.url).href;
const fallbackCover4 = new URL('../assets/cat5.jpg', import.meta.url).href;

const mockSongs: Song[] = [
  { id: 1, title: 'Midnight Dreams',  artist: 'Luna & Co',       duration: 234, albumArt: fallbackCover1, streamUrl: `${API_BASE}/audio/track1.mp3` },
  { id: 2, title: 'Lavender Fields',  artist: 'Ethereal Sounds', duration: 189, albumArt: fallbackCover2, streamUrl: `${API_BASE}/audio/track2.mp3` },
  { id: 3, title: 'Purple Haze',      artist: 'Violet Sky',      duration: 205, albumArt: fallbackCover3, streamUrl: `${API_BASE}/audio/track3.mp3` },
  { id: 4, title: 'Second Flower',    artist: 'Purple whiskers', duration: 177, albumArt: fallbackCover4, streamUrl: `${API_BASE}/audio/track4.mp3` },
];


const formatTime = (sec: number): string => {
  const s = Math.max(0, Math.floor(sec || 0));
  const m = Math.floor(s / 60);
  const r = s % 60;
  return `${m}:${r.toString().padStart(2, '0')}`;
};

export function SongPlayer() {
  const [songs, setSongs] = useState<Song[]>(mockSongs);
  const [currentSongIndex, setCurrentSongIndex] = useState(0);
  const [isPlaying, setIsPlaying] = useState(false);
  const [currentTime, setCurrentTime] = useState(0);
  const [duration, setDuration] = useState(0);
  const [volume, setVolume] = useState<[number]>(() => {
    const saved = Number(localStorage.getItem(VOLUME_KEY));
    return [Number.isFinite(saved) ? saved : 75];
  });
  const [isLiked, setIsLiked] = useState(false);
  const [isShuffled, setIsShuffled] = useState(false);
  const [isRepeating, setIsRepeating] = useState(false);

  const audioRef = useRef<HTMLAudioElement | null>(null);
  const currentSong = songs[currentSongIndex];

  // API laden (erst /SongApi/songs, sonst /songs)
  useEffect(() => {
    const load = async () => {
      try {
        let res = await fetch(`${API_BASE}/SongApi/songs`);
        if (!res.ok) throw new Error('fallback');
        const data: Song[] = await res.json();
        if (Array.isArray(data) && data.length) setSongs(normalizeSongs(data));
      } catch {
        try {
          const res2 = await fetch(`${API_BASE}/songs`);
          if (!res2.ok) return;
          const data2: Song[] = await res2.json();
          if (Array.isArray(data2) && data2.length) setSongs(normalizeSongs(data2));
        } catch {}
      }
    };
    load();
  }, []);

  // Songwechsel: Quelle + gemerkte Lautstärke setzen, ggf. abspielen
  useEffect(() => {
    const audio = audioRef.current;
    if (!audio || !currentSong) return;

    audio.src = ensureAbsolute(currentSong.streamUrl ?? `${API_BASE}/audio/track${currentSong.id}.mp3`);
    audio.load();

    // ⬇️ Lautstärke direkt anwenden (persistenter Wert)
    audio.volume = (volume?.[0] ?? 75) / 100;

    setCurrentTime(0);
    if (isPlaying) {
      audio.play().catch(() => setIsPlaying(false));
    }
  }, [currentSongIndex, songs]); // volume absichtlich nicht in deps

  // Lautstärke-Änderungen sofort anwenden + speichern
  useEffect(() => {
    const audio = audioRef.current;
    if (audio) audio.volume = (volume?.[0] ?? 75) / 100;
    localStorage.setItem(VOLUME_KEY, String(volume[0]));
  }, [volume]);

  const handleVolumeChange = (v: number[]) => {
    const val = Math.min(100, Math.max(0, v[0] ?? 0));
    setVolume([val]);
    // (Persistierung übernimmt der useEffect oben)
  };

  const togglePlayPause = () => {
    const audio = audioRef.current;
    if (!audio) return;
    if (isPlaying) {
      audio.pause();
      setIsPlaying(false);
    } else {
      audio.play().then(() => setIsPlaying(true)).catch(() => setIsPlaying(false));
    }
  };

  const nextSong = () => {
    if (songs.length === 0) return;
    if (isShuffled) {
      const rand = Math.floor(Math.random() * songs.length);
      setCurrentSongIndex(rand);
    } else {
      setCurrentSongIndex((prev) => (prev + 1) % songs.length);
    }
    setCurrentTime(0);
  };

  const previousSong = () => {
    if (songs.length === 0) return;
    setCurrentSongIndex((prev) => (prev - 1 + songs.length) % songs.length);
    setCurrentTime(0);
  };

  const handleProgressChange = (value: number[]) => {
    const audio = audioRef.current;
    if (!audio) return;
    const t = value?.[0] ?? 0;
    audio.currentTime = t;
    setCurrentTime(t);
  };

  // Audio-Events
  const onTimeUpdate = () => {
    const audio = audioRef.current;
    if (audio) setCurrentTime(audio.currentTime);
  };

  const onLoadedMetadata = () => {
    const audio = audioRef.current;
    const d = audio?.duration && !Number.isNaN(audio.duration) ? audio.duration : currentSong?.duration ?? 0;
    setDuration(d);

    // ⬇️ Sicherheit: Lautstärke nach load nochmal setzen
    if (audio) audio.volume = (volume?.[0] ?? 75) / 100;
  };

  const onEnded = () => {
    if (isRepeating) {
      const audio = audioRef.current;
      if (!audio) return;
      audio.currentTime = 0;
      audio.play().catch(() => setIsPlaying(false));
    } else {
      nextSong();
    }
  };

  return (
    <div className="w-full max-w-sm mx-auto bg-gray-900 rounded-2xl shadow-2xl p-8 border border-gray-800 backdrop-blur-sm">
      {/* Hidden audio */}
      <audio
        ref={audioRef}
        onTimeUpdate={onTimeUpdate}
        onLoadedMetadata={onLoadedMetadata}
        onEnded={onEnded}
      />

      {/* Album Art */}
      <div className="relative mb-6">
        <div className="w-full aspect-square rounded-xl overflow-hidden shadow-lg relative">
          <ImageWithFallback
            src={ensureAbsolute(currentSong.albumArt)}
            alt={`${currentSong.title} album art`}
            className="w-full h-full object-cover"
          />
          <div className="absolute inset-0 bg-gradient-to-t from-black/20 via-transparent to-transparent"></div>
        </div>

        {/* Like */}
        <Button
          variant="ghost"
          size="icon"
          className={`absolute top-3 right-3 rounded-full bg-black/50 backdrop-blur-sm hover:bg-black/70 border-0 transition-all duration-200 ${isLiked ? 'text-purple-400' : 'text-gray-300'}`}
          onClick={() => setIsLiked((v) => !v)}
        >
          <Heart className={`h-4 w-4 ${isLiked ? 'fill-current' : ''}`} />
        </Button>
      </div>

      {/* Info */}
      <div className="text-center mb-6">
        <h2 className="text-white mb-1 truncate">{currentSong.title}</h2>
        <p className="text-purple-300 text-sm opacity-80">{currentSong.artist}</p>
      </div>

      {/* Progress */}
      <div className="mb-6">
        <Slider
          value={[Math.min(currentTime, duration || currentSong.duration)]}
          max={Math.max(duration || 0, currentSong.duration)}
          step={1}
          onValueChange={handleProgressChange}
          className="w-full [&>span:first-child]:bg-gray-700 [&_[role=slider]]:border-purple-400 [&_[role=slider]]:bg-purple-500 [&_[role=slider]]:shadow-lg [&_[role=slider]]:shadow-purple-500/25"
        />
        <div className="flex justify-between mt-2 text-xs text-gray-400">
          <span>{formatTime(currentTime)}</span>
          <span>{formatTime(duration || currentSong.duration)}</span>
        </div>
      </div>

      {/* Controls */}
      <div className="flex items-center justify-center gap-4 mb-6">
        <Button
          variant="ghost"
          size="icon"
          className={`rounded-full w-10 h-10 transition-colors ${isShuffled ? 'text-purple-400 bg-purple-400/10 hover:bg-purple-400/20' : 'text-gray-400 hover:text-purple-300 hover:bg-gray-800'}`}
          onClick={() => setIsShuffled((v) => !v)}
        >
          <Shuffle className="h-4 w-4" />
        </Button>

        <Button variant="ghost" size="icon" className="rounded-full text-gray-300 hover:text-white hover:bg-gray-800 w-10 h-10" onClick={previousSong}>
          <SkipBack className="h-5 w-5" />
        </Button>

        <Button
          size="icon"
          className="h-12 w-12 rounded-full bg-purple-600 hover:bg-purple-700 shadow-lg shadow-purple-600/25 transition-all duration-200 hover:scale-105"
          onClick={togglePlayPause}
        >
          {isPlaying ? <Pause className="h-5 w-5 text-white" /> : <Play className="h-5 w-5 text-white ml-0.5" />}
        </Button>

        <Button variant="ghost" size="icon" className="rounded-full text-gray-300 hover:text-white hover:bg-gray-800 w-10 h-10" onClick={nextSong}>
          <SkipForward className="h-5 w-5" />
        </Button>

        <Button
          variant="ghost"
          size="icon"
          className={`rounded-full w-10 h-10 transition-colors ${isRepeating ? 'text-purple-400 bg-purple-400/10 hover:bg-purple-400/20' : 'text-gray-400 hover:text-purple-300 hover:bg-gray-800'}`}
          onClick={() => setIsRepeating((v) => !v)}
        >
          <Repeat className="h-4 w-4" />
        </Button>
      </div>

      {/* Volume */}
      <div className="flex items-center gap-3 mb-4">
        <Volume2 className="h-4 w-4 text-gray-400 flex-shrink-0" />
        <Slider
          value={volume}
          max={100}
          step={1}
          onValueChange={handleVolumeChange}  // ⬅️ benutzt jetzt den Handler
          className="flex-1 [&>span:first-child]:bg-gray-700 [&_[role=slider]]:border-purple-400 [&_[role=slider]]:bg-purple-500 [&_[role=slider]]:shadow-lg [&_[role=slider]]:shadow-purple-500/25"
        />
        <span className="text-xs text-gray-400 w-8 text-right">{volume[0]}</span>
      </div>

      {/* Queue dots */}
      <div className="flex justify-center gap-2">
        {songs.map((_, index) => (
          <button
            key={index}
            onClick={() => { setCurrentSongIndex(index); setCurrentTime(0); }}
            className={`w-2 h-2 rounded-full transition-all duration-200 ${index === currentSongIndex ? 'bg-purple-500' : 'bg-gray-600 hover:bg-gray-500'}`}
          />
        ))}
      </div>
    </div>
  );
}

// --- Helpers ---
function ensureAbsolute(url: string | undefined): string {
  if (!url) return '';
  if (url.startsWith('http://') || url.startsWith('https://')) return url;
  return `${API_BASE}${url.startsWith('/') ? '' : '/'}${url}`;
}

function normalizeSongs(list: Song[]): Song[] {
  return list.map(s => ({
    ...s,
    albumArt: ensureAbsolute(s.albumArt),
    streamUrl: s.streamUrl ? ensureAbsolute(s.streamUrl) : `${API_BASE}/audio/track${s.id}.mp3`,
  }));
}
