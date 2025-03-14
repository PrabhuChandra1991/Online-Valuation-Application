import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class LoginService {

  private apiUrl = environment.apiURL; // Update this with your actual API URL

    private httpOptions = {
        headers: new HttpHeaders({
          'Content-Type': 'application/json'
        })
      };

    constructor(private http: HttpClient) { }

    
    requestPassword(user: any): Observable<any> {
        return this.http.post(`${this.apiUrl}/api/Login/request-temp-password`, JSON.stringify(user), this.httpOptions);
      }

      login(user: any): Observable<any> {
        return this.http.post(`${this.apiUrl}/api/Login/validate-temp-password?tempPassword=${user.tempPassword}`,JSON.stringify(user), this.httpOptions);
      }
    
     
}
