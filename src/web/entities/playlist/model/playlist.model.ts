import mongoose, { Schema, Document } from 'mongoose'

export interface IPlaylistItem {
  _id: string
  songId: mongoose.Types.ObjectId
  keyOverride?: string
  bpmOverride?: number
  structureOverride?: string[]
  levels: { [stemGroupId: string]: number }
  pans: { [stemGroupId: string]: { L: number; R: number } }
  order: number
}

export interface ITransition {
  _id: string
  type: 'autostart' | 'pause' | 'crossfade'
  params: {
    pauseDuration?: number
    crossfadeDuration?: number
  }
}

export interface IPlaylist extends Document {
  _id: string
  bandId: mongoose.Types.ObjectId
  title: string
  description?: string
  eventDateTime?: Date
  venue?: string
  items: IPlaylistItem[]
  transitions: ITransition[]
  contentVersion: number
  status: 'draft' | 'published'
  createdAt: Date
  updatedAt: Date
}

const playlistItemSchema = new Schema<IPlaylistItem>({
  songId: {
    type: Schema.Types.ObjectId,
    ref: 'Song',
    required: true
  },
  keyOverride: {
    type: String,
    enum: ['C', 'C#', 'D', 'D#', 'E', 'F', 'F#', 'G', 'G#', 'A', 'A#', 'B']
  },
  bpmOverride: {
    type: Number,
    min: 30,
    max: 300
  },
  structureOverride: [String],
  levels: {
    type: Map,
    of: Number,
    default: new Map()
  },
  pans: {
    type: Map,
    of: {
      L: { type: Number, min: 0, max: 1 },
      R: { type: Number, min: 0, max: 1 }
    },
    default: new Map()
  },
  order: {
    type: Number,
    required: true
  }
})

const transitionSchema = new Schema<ITransition>({
  type: {
    type: String,
    enum: ['autostart', 'pause', 'crossfade'],
    required: true
  },
  params: {
    pauseDuration: {
      type: Number,
      min: 0
    },
    crossfadeDuration: {
      type: Number,
      min: 0
    }
  }
})

const playlistSchema = new Schema<IPlaylist>({
  bandId: {
    type: Schema.Types.ObjectId,
    ref: 'Band',
    required: true
  },
  title: {
    type: String,
    required: true,
    trim: true,
    maxlength: 200
  },
  description: {
    type: String,
    maxlength: 1000
  },
  eventDateTime: {
    type: Date
  },
  venue: {
    type: String,
    trim: true,
    maxlength: 200
  },
  items: [playlistItemSchema],
  transitions: [transitionSchema],
  contentVersion: {
    type: Number,
    default: 1
  },
  status: {
    type: String,
    enum: ['draft', 'published'],
    default: 'draft'
  }
}, {
  timestamps: true
})

export const Playlist = mongoose.models.Playlist || mongoose.model<IPlaylist>('Playlist', playlistSchema)
