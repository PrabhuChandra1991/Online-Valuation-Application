interface UserCourse {
    userCourseId: number;
    userId: number;
    courseName: string;
    degreeTypeId: number;
    numberOfYearsHandled: number;
    isHandledInLast2Semester: boolean;
    isActive: boolean;
    createdById: number;
    createdDate: string;
    modifiedById: number;
    modifiedDate: string;
  }
  
  interface UserQualification {
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
  
  interface UserDesignation {
    userDesignationId: number;
    designationId: number;
    userId: number;
    experience: number;
    isCurrent: boolean;
    isActive: boolean;
    createdById: number;
    createdDate: string;
    modifiedById: number;
    modifiedDate: string;
  }

  interface UserAreaOfSpecialization {
    userAreaOfSpecializationId: number;
    userId: number;
    areaOfSpecializationName: string;
    isActive: boolean;
    createdDate: string;  // Can be Date type if needed
    createdById: number;
    modifiedDate: string;  // Can be Date type if needed
    modifiedById: number;
  }
  
  export interface UserProfile {
    userId: number;
    name: string;
    email: string;
    gender: string;
    salutation: string;
    mobileNumber: string;
    roleId: number;
    totalExperience: number;
    departmentName: string;
    collegeName: string;
    bankAccountName: string;
    bankAccountNumber: string;
    bankName: string;
    bankBranchName: string;
    bankIFSCCode: string;
    isEnabled: boolean;
    userCourses: UserCourse[];
    userAreaOfSpecializations: UserAreaOfSpecialization[]; 
    userQualifications: UserQualification[];
    userDesignations: UserDesignation[];
    isActive: boolean;
    createdById: number;
    createdDate: string;
    modifiedById: number;
    modifiedDate: string;
  }
  