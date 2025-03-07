import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})

export class UserService {

    private apiUrl = 'http://localhost:5088'; // Update this with your actual API URL

    constructor(private http: HttpClient) { }

    getUser(userId: string): Observable<any> {
        return this.http.get(`${this.apiUrl}/api/User/${userId}`);
    }

    getUsers(): Observable<any> {
        return this.http.get(`${this.apiUrl}/api/User`);
    }

}