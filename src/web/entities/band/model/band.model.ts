import mongoose, { Schema, Document } from 'mongoose'

export interface IBandRole {
  _id: string
  name: string
  permissions: string[]
}

export interface IBandMember {
  user: mongoose.Types.ObjectId
  role: string
  joinedAt: Date
  status: 'active' | 'invited' | 'left'
}

export interface IBand extends Document {
  _id: string
  name: string
  description?: string
  avatar?: string
  owner: mongoose.Types.ObjectId
  members: IBandMember[]
  roles: IBandRole[]
  settings: {
    isPublic: boolean
    inviteOnly: boolean
  }
  createdAt: Date
  updatedAt: Date
}

const bandRoleSchema = new Schema<IBandRole>({
  name: {
    type: String,
    required: true
  },
  permissions: [{
    type: String,
    enum: [
      'read',
      'edit_songs',
      'edit_playlists',
      'manage_members',
      'manage_roles',
      'delete',
      'publish',
      'export',
      'live_control'
    ]
  }]
})

const bandMemberSchema = new Schema<IBandMember>({
  user: {
    type: Schema.Types.ObjectId,
    ref: 'User',
    required: true
  },
  role: {
    type: String,
    required: true,
    default: 'viewer'
  },
  joinedAt: {
    type: Date,
    default: Date.now
  },
  status: {
    type: String,
    enum: ['active', 'invited', 'left'],
    default: 'active'
  }
})

const bandSchema = new Schema<IBand>({
  name: {
    type: String,
    required: true,
    trim: true,
    maxlength: 100
  },
  description: {
    type: String,
    maxlength: 500
  },
  avatar: {
    type: String
  },
  owner: {
    type: Schema.Types.ObjectId,
    ref: 'User',
    required: true
  },
  members: [bandMemberSchema],
  roles: {
    type: [bandRoleSchema],
    default: [
      {
        name: 'owner',
        permissions: ['read', 'edit_songs', 'edit_playlists', 'manage_members', 'manage_roles', 'delete', 'publish', 'export', 'live_control']
      },
      {
        name: 'admin',
        permissions: ['read', 'edit_songs', 'edit_playlists', 'manage_members', 'manage_roles', 'publish', 'export', 'live_control']
      },
      {
        name: 'editor',
        permissions: ['read', 'edit_songs', 'edit_playlists', 'export']
      },
      {
        name: 'player',
        permissions: ['read', 'live_control']
      },
      {
        name: 'viewer',
        permissions: ['read']
      }
    ]
  },
  settings: {
    isPublic: {
      type: Boolean,
      default: false
    },
    inviteOnly: {
      type: Boolean,
      default: true
    }
  }
}, {
  timestamps: true
})

export const Band = mongoose.models.Band || mongoose.model<IBand>('Band', bandSchema)
