export interface UserCourse {
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