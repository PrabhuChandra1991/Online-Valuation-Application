export interface AnswersheetMark {
  answersheetId: number;
  questionNumber: number;
  questionNumberSubNum: number;
  maximumMark: number;
  obtainedMark: number;
  createdById: number;
  modifiedById: number;
}

export interface AnswersheetAllocateInputModel {
  examinationId: number;
  userId: number;
  noofsheets: number;
}