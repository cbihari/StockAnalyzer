# Learning Center

## Purpose

Teach beginner-friendly stock analysis concepts such as indicators, risk, and model reliability.

## Business Flow

1. User opens learning content.
2. Lessons explain terms used elsewhere in the app.
3. Progress is stored locally for the guest browser.

## UI Flow

- Routes: `/learn`, `/learn/:slug`.
- Component: `learning-center.component.ts`.
- Content: `learning-content.ts`.
- Progress: `LearningProgressService`.

## Backend Flow

None currently. Learning content is frontend-local.

## API Endpoints

None.

## Models

- `LearningLesson`
- Local progress set in `LearningProgressService`.

## Validation

- Unknown lesson slug should fall back to the learning overview or a safe empty state.

## Database Tables

None.

## Error Handling

- Local storage failures should not block reading lessons.

## Future Improvements

- Server-backed authenticated learning progress.
- Contextual links from indicators and model warnings to relevant lessons.
- More India-specific beginner examples.
