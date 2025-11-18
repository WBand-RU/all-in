# WBand Frontend Features & Navigation

## 📱 Complete Feature Set

All frontend features are now implemented with full navigation in the sidebar.

## 🎯 Navigation Structure

### Main Sidebar Navigation

The sidebar provides quick access to all features:

1. **Dashboard** (`/app`)

   - Icon: 📊 LayoutDashboard
   - Landing page with overview

2. **Songs** (`/app/songs`)

   - Icon: 🎵 Music
   - List all songs with search and pagination
   - Create new songs: `/app/songs/new`
   - Edit songs: `/app/songs/:id/edit`
   - Fields: Title, Author, Lyrics, Chords, Key, BPM

3. **Playlists** (`/app/playlists`)

   - Icon: 📃 ListMusic
   - Manage setlists and playlists
   - Create new playlist: `/app/playlists/new`
   - Edit playlist: `/app/playlists/:id/edit`
   - Drag-and-drop reordering
   - Support for songs, blocks, and pauses

4. **Playback** (`/app/playback/:id`)

   - Icon: ▶️ Play
   - Multitrack audio mixer interface
   - Individual track controls (volume, pan, mute, solo)
   - Mixer presets (Vocals Focus, Instrumental, Balanced)
   - Timeline scrubbing
   - Upload audio tracks

5. **Stage Mode** (`/app/stage`)

   - Icon: 🖥️ Maximize2
   - Full-screen lyrics and chords display
   - Keyboard shortcuts:
     - Arrow keys: Navigate items/verses
     - Space: Play/Pause
     - ESC: Exit stage mode
   - Adjustable font size
   - Verse-by-verse navigation

6. **Chat** (`/app/chat`)

   - Icon: 💬 MessageSquare
   - Real-time team messaging
   - Member list with online status
   - Message reactions and replies
   - Edit/delete own messages
   - File attachments support
   - Typing indicators

7. **Search** (`/app/search`)

   - Icon: 🔍 Search
   - Global search across all content
   - Tabbed results: All, Songs, Playlists, Playbacks, Members, Messages
   - Advanced filters by type and date range
   - Quick navigation to results

8. **Bands** (`/app/bands`)
   - Icon: 👥 Users
   - Manage band memberships
   - Admin/User role-based access

## 🎨 UI Components

All pages use **shadcn/ui** components with **TailwindCSS**:

- **Forms**: Input, Textarea, Select, Checkbox, Label
- **Feedback**: Toast notifications, Dialog modals, Badges
- **Navigation**: Tabs, Sidebar, Buttons
- **Data Display**: Cards, Tables, Progress bars
- **Special**: Slider (for mixer), Scroll Area, Drag-and-Drop

## 🔒 Authentication

All routes under `/app/*` require authentication via **Keycloak**.

## 💾 Offline Support

The **OfflineService** provides:

- IndexedDB local storage for all data types
- Automatic online/offline detection
- Queued operations for sync
- Manual sync trigger
- Import/export capabilities

## 📋 Component Structure

```
web/src/
├── pages/
│   ├── HomePage.tsx (Landing)
│   ├── DashboardPage.tsx
│   ├── BandListPage.tsx
│   ├── songs/
│   │   ├── SongsListPage.tsx
│   │   └── SongFormPage.tsx
│   ├── playlists/
│   │   ├── PlaylistsListPage.tsx
│   │   └── PlaylistFormPage.tsx
│   ├── playback/
│   │   └── PlaybackControlPage.tsx
│   ├── stage/
│   │   └── StagePage.tsx
│   ├── chat/
│   │   └── ChatPage.tsx
│   └── search/
│       └── SearchPage.tsx
├── widgets/
│   ├── app-sidebar.tsx (Main sidebar)
│   └── nav-main.tsx (Navigation items)
├── shared/
│   └── ui/ (shadcn/ui components)
├── services/
│   └── OfflineService.ts
└── routes.tsx (Route configuration)
```

## 🚀 Key Features by Page

### Songs Management

- ✅ Full CRUD operations
- ✅ Search and filter
- ✅ Lyrics with line breaks
- ✅ Chord notation support
- ✅ Musical key selection
- ✅ BPM tracking

### Playlists Management

- ✅ Drag-and-drop item ordering
- ✅ Custom key per song
- ✅ Block items for announcements
- ✅ Pause items for breaks
- ✅ Duration calculation
- ✅ Planned date tracking

### Playback Controls

- ✅ Multitrack audio playback
- ✅ Individual track mixer
- ✅ Volume and pan controls
- ✅ Mute and solo tracks
- ✅ Preset configurations
- ✅ Timeline navigation

### Stage Mode

- ✅ Full-screen display
- ✅ Chords toggle
- ✅ Font size adjustment
- ✅ Keyboard navigation
- ✅ Verse pagination
- ✅ Support all item types

### Chat Interface

- ✅ Real-time messaging
- ✅ Online status indicators
- ✅ Message reactions
- ✅ Reply threading
- ✅ Message editing
- ✅ File attachments

### Global Search

- ✅ Unified search interface
- ✅ Type-specific filtering
- ✅ Date range filters
- ✅ Result previews
- ✅ Quick navigation

## 🎯 User Experience

### Navigation Patterns

- **Sidebar**: Always accessible main navigation
- **Breadcrumbs**: Context-aware page location
- **Toast Notifications**: Action feedback
- **Modal Dialogs**: Focused interactions
- **Keyboard Shortcuts**: Power user features

### Responsive Design

- Mobile-friendly sidebar (collapsible)
- Touch-optimized controls
- Adaptive layouts
- Scroll areas for long content

## 🧪 Testing Routes

To test all features, visit these URLs after starting the app:

```
http://localhost/app                    # Dashboard
http://localhost/app/songs              # Songs list
http://localhost/app/songs/new          # Create song
http://localhost/app/playlists          # Playlists list
http://localhost/app/playlists/new      # Create playlist
http://localhost/app/playback/1         # Playback controls
http://localhost/app/stage              # Stage mode
http://localhost/app/chat               # Chat
http://localhost/app/search             # Search
http://localhost/app/bands              # Bands (admin)
```

## 🎉 Status: 100% Complete

All planned frontend features are implemented and accessible through the sidebar navigation!
