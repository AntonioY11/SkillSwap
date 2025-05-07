-- Create the database
CREATE DATABASE SkillSwap;
USE SkillSwap;

-- Create User table
CREATE TABLE User (
    User_id INT PRIMARY KEY AUTO_INCREMENT,
    FullName VARCHAR(255) NOT NULL,
    Email VARCHAR(255) UNIQUE NOT NULL,
    Password VARCHAR(255) NOT NULL
);

-- Create Posts table
CREATE TABLE Posts (
    Post_id INT PRIMARY KEY AUTO_INCREMENT,
    Title VARCHAR(255) NOT NULL,
    Description TEXT,
    Image VARCHAR(255),
    User_id INT NOT NULL,
    FOREIGN KEY (User_id) REFERENCES User(User_id) ON DELETE CASCADE
);

-- Create Request table
CREATE TABLE Request (
    Request_id INT PRIMARY KEY AUTO_INCREMENT,
    Status TINYINT(1) DEFAULT NULL, -- NULL for pending, 0 for rejected, 1 for accepted
    Comment TEXT,
    User_id INT NOT NULL,
    Post_id INT NOT NULL,
    FOREIGN KEY (User_id) REFERENCES User(User_id) ON DELETE CASCADE,
    FOREIGN KEY (Post_id) REFERENCES Posts(Post_id) ON DELETE CASCADE
);