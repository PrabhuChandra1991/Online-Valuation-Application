export interface UserQualification {
    userQualificationId: number;
    userId: number;
    title: string;
    name: string;
    specialization: string;
    isCompleted: boolean;
    isActive: boolean;
    createdById: number;
    createdDate: string;
    modifiedById: number;
    modifiedDate: string;
  }