import mongoose, { Schema, Document } from 'mongoose'

export interface IBandInvitation extends Document {
  _id: string
  bandId: mongoose.Types.ObjectId
  invitedBy: mongoose.Types.ObjectId
  email: string
  role: string
  status: 'pending' | 'accepted' | 'declined' | 'expired'
  token: string
  expiresAt: Date
  createdAt: Date
  updatedAt: Date
}

export interface IInviteToken extends Document {
  _id: string
  bandId: mongoose.Types.ObjectId
  createdBy: mongoose.Types.ObjectId
  token: string
  role: string
  maxUses: number
  currentUses: number
  expiresAt: Date
  isActive: boolean
  createdAt: Date
  updatedAt: Date
}

const bandInvitationSchema = new Schema<IBandInvitation>({
  bandId: {
    type: Schema.Types.ObjectId,
    ref: 'Band',
    required: true
  },
  invitedBy: {
    type: Schema.Types.ObjectId,
    ref: 'User',
    required: true
  },
  email: {
    type: String,
    required: true,
    lowercase: true,
    trim: true
  },
  role: {
    type: String,
    required: true,
    default: 'viewer'
  },
  status: {
    type: String,
    enum: ['pending', 'accepted', 'declined', 'expired'],
    default: 'pending'
  },
  token: {
    type: String,
    required: true,
    unique: true
  },
  expiresAt: {
    type: Date,
    required: true,
    default: () => new Date(Date.now() + 7 * 24 * 60 * 60 * 1000) // 7 дней
  }
}, {
  timestamps: true
})

const inviteTokenSchema = new Schema<IInviteToken>({
  bandId: {
    type: Schema.Types.ObjectId,
    ref: 'Band',
    required: true
  },
  createdBy: {
    type: Schema.Types.ObjectId,
    ref: 'User',
    required: true
  },
  token: {
    type: String,
    required: true,
    unique: true
  },
  role: {
    type: String,
    required: true,
    default: 'viewer'
  },
  maxUses: {
    type: Number,
    default: 1
  },
  currentUses: {
    type: Number,
    default: 0
  },
  expiresAt: {
    type: Date,
    required: true,
    default: () => new Date(Date.now() + 7 * 24 * 60 * 60 * 1000) // 7 дней
  },
  isActive: {
    type: Boolean,
    default: true
  }
}, {
  timestamps: true
})

// Индексы для производительности
bandInvitationSchema.index({ bandId: 1, email: 1 })
bandInvitationSchema.index({ token: 1 })
bandInvitationSchema.index({ expiresAt: 1 }, { expireAfterSeconds: 0 })

inviteTokenSchema.index({ bandId: 1 })
inviteTokenSchema.index({ token: 1 })
inviteTokenSchema.index({ expiresAt: 1 }, { expireAfterSeconds: 0 })

export const BandInvitation = mongoose.models.BandInvitation || mongoose.model<IBandInvitation>('BandInvitation', bandInvitationSchema)
export const InviteToken = mongoose.models.InviteToken || mongoose.model<IInviteToken>('InviteToken', inviteTokenSchema)
