import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})

export class DashboardService {

    private apiUrl = 'http://localhost:5088'; // Update this with your actual API URL

    private httpOptions = {
        headers: new HttpHeaders({
          'Content-Type': 'application/json'
        })
      };

    constructor(private http: HttpClient) { }

    getCourses(): Observable<any> {
        return this.http.get(`${this.apiUrl}/api/Course`);
    }

    getDepartments(): Observable<any> {
        return this.http.get(`${this.apiUrl}/api/Department`);
    }
}
