export interface User {
    id: number;
    name?: string;
    email: string;
    mobileNumber?: string;
    roleId?: number;
    workExperience?: number;
    departmentId?: number;
    designationId?: number;
    collegeName?: string;
    bankAccountName?: string;
    bankAccountNumber?: string;
    bankName?: string;
    bankBranchName?: string;
    isEnabled: boolean;
    qualification?: string;
    areaOfSpecialization?: string;
    courseId: number;
    bankIfscCode?: string;
    isActive: boolean;
    createdById: number;
    createdDate: string;  // ISO string format (e.g., "2025-03-08T12:24:12.945Z")
    modifiedById: number;
    modifiedDate?: string; // Nullable, so it is optional (?)
  }
  