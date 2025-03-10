export interface User {
  id: number;
  name: string;
  email: string;
  mobileNumber: string;
  roleId: number;
  totalExperience: number;
  departmentId: number;
  collegeName: string;
  bankAccountName: string;
  bankAccountNumber: string;
  bankName: string;
  bankBranchName: string;
  bankIFSCCode: string;
  isEnabled: boolean;
  userCourses: any[]; // Assuming empty array
  userAreaOfSpecializations: UserAreaOfSpecialization[];
  userQualifications: UserQualification[];
  userDesignations: any[]; // Assuming empty array
  isActive: boolean;
  createdById: number;
  createdDate: string;
  modifiedById: number;
  modifiedDate: string;
}

export interface UserAreaOfSpecialization {
  id: number;
  userAreaOfSpecializationId: number;
  userId: number;
  areaOfSpecializationName: string;
  isActive: boolean;
  createdById: number;
  createdDate: string;
  modifiedById: number;
  modifiedDate: string;
}

export interface UserQualification {
  id: number;
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
