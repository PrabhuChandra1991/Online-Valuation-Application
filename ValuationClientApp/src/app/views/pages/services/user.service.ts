import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

@Injectable({
    providedIn: 'root'
})

export class UserService {

    private apiUrl = environment.apiURL; // Update this with your actual API URL

    private httpOptions = {
        headers: new HttpHeaders({
          'Content-Type': 'application/json'
        })
      };

    constructor(private http: HttpClient) { }

    getUser(userId: string): Observable<any> {
        return this.http.get(`${this.apiUrl}/api/User/${userId}`);
    }

    getUsers(): Observable<any> {
        return this.http.get(`${this.apiUrl}/api/User`);
    }

    addUser(user: any): Observable<any> {
        return this.http.post(`${this.apiUrl}/api/User`, JSON.stringify(user), this.httpOptions);
      }
    
      updateUser(userId: string, user: any): Observable<any> {
        return this.http.put(`${this.apiUrl}/api/User/${userId}`, JSON.stringify(user), this.httpOptions);
      }

}