import { LEARNING_LESSONS, findLesson } from './learning-content';

describe('learning content', () => {
  it('contains every required foundational lesson', () => {
    expect(LEARNING_LESSONS.map((lesson) => lesson.slug)).toEqual(['rsi', 'macd', 'ema', 'sma', 'volume', 'risk']);
  });

  it('finds a lesson by stable slug', () => {
    expect(findLesson('rsi')?.title).toContain('RSI');
    expect(findLesson('missing')).toBeUndefined();
  });
});
