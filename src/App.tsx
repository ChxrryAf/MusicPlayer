import { SongPlayer } from './components/SongPlayer';

export default function App() {
  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-900 via-purple-900/20 to-black flex items-center justify-center p-4">
      {/* Subtle background gradient overlay */}
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_50%_50%,rgba(147,51,234,0.1),transparent_70%)] pointer-events-none" />
      
      <div className="relative z-10">
        <SongPlayer />
      </div>
    </div>
  );
}