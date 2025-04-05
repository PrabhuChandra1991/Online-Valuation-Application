import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class TemplateManagementService {

   private apiUrl = environment.apiURL; // Update this with your actual API URL

     private httpOptions = {
         headers: new HttpHeaders({
           'Content-Type': 'application/json'
         })
       };

  constructor(private http: HttpClient) {}

  // Fetch courses from API
  getCourses(): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/Course`);
  }

  // Fetch QP Template by Course ID
  getQPTemplateByCourseId(courseId: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/QpTemplate/GetQPTemplateByCourseId/${courseId}`);
  }

  uploadDocument(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post(`${this.apiUrl}/api/BlobStorage/upload`, formData);
  }

  deleteDocument(fileName: string): Observable<any> {
    const encodedFileName = encodeURIComponent(fileName);
    return this.http.delete(`${this.apiUrl}/api/BlobStorage/delete/${fileName}`);
  }

  saveQpTemplate(qpTemplateData: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/QpTemplate`, qpTemplateData);
  }

   // Fetch templated from API
   getTemplates(institutionId:number): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/QpTemplate/GetQPTemplates/${institutionId}`);
  }
  // Fetch templated from API
  getTemplatesByStatusId(statusId:number): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/QpTemplate/GetQPTemplatesByStatusId/${statusId}`);
  }

  getQpTemplateById(qpTemplateId: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/api/QpTemplate/${qpTemplateId}`);
  }

  assignQpTemplateToUser(userId: any,templateId: any): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/QpTemplate/AssignQPForGeneration/${userId}/${templateId}`);
  }

  getAssignedQpTemplateByUser(userId: any): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/QpTemplate/GetUserQPTemplates/${userId}`);
  }

  CreateQpTemplate(formData: FormData): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/QpTemplate/CreateQpTemplate`,formData);
  }
  getExpertsForQPAssignment(): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/QpTemplate/GetExpertsForQPAssignment`);
  }

  updateAssignment(qpTemplateId: any,formData: FormData): Observable<any> {
    return this.http.put(`${this.apiUrl}/api/UpdateQpTemplate/${qpTemplateId}`, JSON.stringify(formData), this.httpOptions);
  }

  
  printQPTemplate(userqpTemplateId : number): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/QpTemplate/PrintSelectedQP/${userqpTemplateId}/'12345'/true`);
  }

  assignScrutinity(userId:number, userQPTemplateId:number): Observable<any> {
    // return this.http.get(this.apiUrl+'/api/QpTemplate/AssignQPForScrutinity/1/1'); 
    return this.http.get(`${this.apiUrl}/api/QpTemplate/AssignQPForScrutinity/${userId}/${userQPTemplateId}`);
  }

}
