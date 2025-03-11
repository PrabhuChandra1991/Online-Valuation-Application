export interface UserDesignation {
    userDesignationId: number;
    designationId: number;
    userId: number;
    experience: number;
    isCurrent: boolean;
    isActive: boolean;
    createdById: number;
    createdDate: string;  // Or Date if you want to use Date type
    modifiedById: number;
    modifiedDate: string;  // Or Date if you want to use Date type
  }
  