import { NgModule } from '@angular/core';
import { LoginComponent } from './login.component';
import { SignupComponent } from './signup.component';

@NgModule({ imports: [LoginComponent, SignupComponent], exports: [LoginComponent, SignupComponent] })
export class AuthModule {}
