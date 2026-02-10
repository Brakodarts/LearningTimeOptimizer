# LearningTimeOptimizer

A C# console application to help manage study time effectively using a dynamic priority algorithm.

I built this project to practice C# fundamentals (OOP, Collections, File I/O) and to solve a personal problem: organizing my self-study schedule for software development.

How it works

This isn't just a todo list. The core logic is based on a dynamic priority system:

You input your available time for the day (in minutes).

You add skills/topics you want to learn, assigning them a priority (1-5) and duration.

The algorithm generates a daily plan, prioritizing high-value tasks that fit your time window.

Anti-Procrastination Logic: If you skip a task during the end-of-day review, its priority automatically increases for the next day. If you complete it, the priority resets to its original value.

Features

Dynamic Scheduling: Automatically fits tasks into your daily time limit.

Priority Escalation: Unfinished tasks become more urgent over time.

Data Persistence: Skills and their current priorities are saved to user_skills.json automatically.

Weekly Forecast: Calculates a projected schedule for the next 7 days based on current priorities.
