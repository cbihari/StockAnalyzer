import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { AuthUser } from '../core/models';
import { AuthCallbackComponent } from './auth-callback.component';
import { AuthService } from './auth.service';

describe('AuthCallbackComponent', () => {
  it('waits for the user session before navigating home', () => {
    const session = new Subject<AuthUser>();
    const auth = { acceptToken: jasmine.createSpy().and.returnValue(session) };
    const router = jasmine.createSpyObj<Router>('Router', ['navigateByUrl', 'navigate']);
    TestBed.configureTestingModule({
      imports: [AuthCallbackComponent],
      providers: [
        { provide: ActivatedRoute, useValue: { snapshot: { fragment: 'token=test-token' } } },
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router },
      ],
    });

    TestBed.createComponent(AuthCallbackComponent).detectChanges();
    expect(auth.acceptToken).toHaveBeenCalledWith('test-token');
    expect(router.navigateByUrl).not.toHaveBeenCalled();

    session.next({ id: '1', email: 'user@example.com', displayName: 'Test User', createdAt: '' });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/');
  });
});
