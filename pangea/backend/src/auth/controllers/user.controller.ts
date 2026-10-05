import { Controller, Get, Put, Body, UseGuards } from '@nestjs/common';
import { AuthService } from '../services/auth.service';
import { JwtAuthGuard } from '../guards/jwt-auth.guard';
import { CurrentUser } from '../decorators/current-user.decorator';
import { User } from '../../users/entities/user.entity';

@Controller('user')
export class UserController {
  constructor(private authService: AuthService) {}

  /**
   * Get current user profile
   * GET /api/user/profile
   */
  @UseGuards(JwtAuthGuard)
  @Get('profile')
  async getProfile(@CurrentUser() user: any): Promise<User> {
    return this.authService.getProfile(user.id);
  }

  /**
   * Update user profile
   * PUT /api/user/profile
   */
  @UseGuards(JwtAuthGuard)
  @Put('profile')
  async updateProfile(
    @CurrentUser() user: any,
    @Body() updateData: { firstName?: string; lastName?: string; email?: string },
  ): Promise<User> {
    return this.authService.updateProfile(user.id, updateData);
  }
}












