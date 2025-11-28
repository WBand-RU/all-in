import mongoose, { Schema, Document } from 'mongoose'

export interface IUser extends Document {
  _id: string
  username: string
  email: string
  password: string
  firstName?: string
  lastName?: string
  avatar?: string
  createdAt: Date
  updatedAt: Date
}

const userSchema = new Schema<IUser>({
  username: {
    type: String,
    required: true,
    unique: true,
    trim: true,
    minlength: 3,
    maxlength: 20
  },
  email: {
    type: String,
    required: true,
    unique: true,
    lowercase: true,
    trim: true
  },
  password: {
    type: String,
    required: true,
    minlength: 6
  },
  firstName: {
    type: String,
    trim: true,
    maxlength: 50
  },
  lastName: {
    type: String,
    trim: true,
    maxlength: 50
  },
  avatar: {
    type: String
  }
}, {
  timestamps: true
})

export const User = mongoose.models.User || mongoose.model<IUser>('User', userSchema)
