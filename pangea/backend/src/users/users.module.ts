import { Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { User } from './entities/user.entity';
import { UserBalance } from './entities/user-balance.entity';

@Module({
  imports: [TypeOrmModule.forFeature([User, UserBalance])],
  exports: [TypeOrmModule],
})
export class UsersModule {}












