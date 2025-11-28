import mongoose, { Schema, Document } from 'mongoose'

export interface ISection {
  _id: string
  name: string
  startBar: number
  barLength: number
  localSignature?: {
    num: number
    den: number
  }
}

export interface IChordSheet {
  body: string
  notation: 'CDEFGAB'
  capo?: number
  transpose?: number
}

export interface IStem {
  _id: string
  groupId: mongoose.Types.ObjectId
  kind: string
  fileRef: string
  version: number
  panL: number
  panR: number
  gain: number
}

export interface IStemGroup {
  _id: string
  name: string
  stems: mongoose.Types.ObjectId[]
}

export interface ISong extends Document {
  _id: string
  bandId: mongoose.Types.ObjectId
  title: string
  authors: string[]
  key: string
  bpm: number
  timeSignature: {
    num: number
    den: number
  }
  sections: ISection[]
  chordSheet?: IChordSheet
  stemGroups: IStemGroup[]
  attachments: {
    documents: string[]
    audio: string[]
    video: string[]
    links: string[]
  }
  status: 'draft' | 'band_published' | 'catalog_published'
  contentVersion: number
  visibility: 'band' | 'link' | 'public'
  createdAt: Date
  updatedAt: Date
}

const sectionSchema = new Schema<ISection>({
  name: {
    type: String,
    required: true,
    trim: true
  },
  startBar: {
    type: Number,
    required: true,
    min: 1
  },
  barLength: {
    type: Number,
    required: true,
    min: 1
  },
  localSignature: {
    num: {
      type: Number,
      min: 1,
      max: 16
    },
    den: {
      type: Number,
      enum: [1, 2, 4, 8, 16]
    }
  }
})

const chordSheetSchema = new Schema<IChordSheet>({
  body: {
    type: String,
    required: true
  },
  notation: {
    type: String,
    enum: ['CDEFGAB'],
    default: 'CDEFGAB'
  },
  capo: {
    type: Number,
    min: 0,
    max: 12
  },
  transpose: {
    type: Number,
    min: -12,
    max: 12
  }
})

const stemSchema = new Schema<IStem>({
  groupId: {
    type: Schema.Types.ObjectId,
    required: true
  },
  kind: {
    type: String,
    required: true
  },
  fileRef: {
    type: String,
    required: true
  },
  version: {
    type: Number,
    default: 1
  },
  panL: {
    type: Number,
    default: 1.0,
    min: 0,
    max: 1
  },
  panR: {
    type: Number,
    default: 1.0,
    min: 0,
    max: 1
  },
  gain: {
    type: Number,
    default: 0
  }
})

const stemGroupSchema = new Schema<IStemGroup>({
  name: {
    type: String,
    required: true,
    trim: true
  },
  stems: [{
    type: Schema.Types.ObjectId,
    ref: 'Stem'
  }]
})

const songSchema = new Schema<ISong>({
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
  authors: [{
    type: String,
    trim: true
  }],
  key: {
    type: String,
    required: true,
    enum: ['C', 'C#', 'D', 'D#', 'E', 'F', 'F#', 'G', 'G#', 'A', 'A#', 'B']
  },
  bpm: {
    type: Number,
    required: true,
    min: 30,
    max: 300
  },
  timeSignature: {
    num: {
      type: Number,
      required: true,
      min: 1,
      max: 16
    },
    den: {
      type: Number,
      required: true,
      enum: [1, 2, 4, 8, 16]
    }
  },
  sections: [sectionSchema],
  chordSheet: chordSheetSchema,
  stemGroups: [stemGroupSchema],
  attachments: {
    documents: [String],
    audio: [String],
    video: [String],
    links: [String]
  },
  status: {
    type: String,
    enum: ['draft', 'band_published', 'catalog_published'],
    default: 'draft'
  },
  contentVersion: {
    type: Number,
    default: 1
  },
  visibility: {
    type: String,
    enum: ['band', 'link', 'public'],
    default: 'band'
  }
}, {
  timestamps: true
})

export const Song = mongoose.models.Song || mongoose.model<ISong>('Song', songSchema)
