import { HiddenPipe } from './hidden.pipe';

describe('HiddenPipe', () => {
  it('create an instance', () => {
    const pipe = new HiddenPipe();
    expect(pipe).toBeTruthy();
  });
});
